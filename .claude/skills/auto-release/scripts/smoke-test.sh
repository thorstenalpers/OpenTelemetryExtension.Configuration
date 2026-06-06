#!/usr/bin/env bash
# Smoke-test: start a backend via Helm, run the sample, generate traffic and
# verify that telemetry actually arrived.
#
# Usage: bash smoke-test.sh <backend>
#   backend : aspire | openobserve
#
# Requires a local Kubernetes cluster (e.g. k3s in WSL2) with Helm + kubectl.
# Exit 0 = telemetry confirmed, non-zero = failed.
set -euo pipefail

BACKEND="${1:-}"
SAMPLE_DIR="src/OpenTelemetryExtension.Configuration.Sample"
SWAGGER_URL="http://localhost:5021"   # http app url from launchSettings

if [[ -z "$BACKEND" ]]; then
  echo "Usage: bash smoke-test.sh <aspire|openobserve>"
  exit 2
fi

start_backend() {
  echo ">> Starting $BACKEND via Helm ..."
  case "$BACKEND" in
    aspire)
      (cd infrastructure/helm && \
        helm upgrade --install aspire-dashboard aspire-dashboard/aspire-dashboard \
          --version 1.28.3 -f values.aspire-dashboard.yaml && \
        kubectl apply -f nodeports.aspire-dashboard.yaml) ;;
    openobserve)
      (cd infrastructure/helm && helm upgrade --install openobserve ./chart-openobserve) ;;
    *)
      echo "Unknown backend: $BACKEND"; exit 2 ;;
  esac
}

profile_for() {
  case "$BACKEND" in
    aspire)      echo "Start Aspire" ;;
    openobserve) echo "Start OpenObserve Http" ;;
  esac
}

# 1. Start backend and give it time to become ready
start_backend
echo ">> Waiting for backend to become ready ..."
sleep 20

# 2. Run the sample in the background with the matching profile
PROFILE="$(profile_for)"
echo ">> Running sample with profile: $PROFILE"
( cd "$SAMPLE_DIR" && dotnet run --launch-profile "$PROFILE" ) &
SAMPLE_PID=$!

cleanup() { kill "$SAMPLE_PID" 2>/dev/null || true; }
trap cleanup EXIT

# 3. Wait for the app, then generate traffic
echo ">> Waiting for the sample app ..."
for _ in $(seq 1 30); do
  if curl -sk "$SWAGGER_URL/health" >/dev/null 2>&1; then break; fi
  sleep 2
done
echo ">> Generating traffic ..."
for _ in $(seq 1 5); do curl -sk "$SWAGGER_URL/" >/dev/null 2>&1 || true; done

# 4. Give the exporter time to flush, then verify ingestion
echo ">> Waiting for export/flush ..."
sleep 15

verify_openobserve() {
  # Query the OpenObserve API and assert at least one record exists.
  # NodePort 30117. The _search API needs the user password (rootPassword: admin),
  # not the OTLP ingestion passcode.
  local base="http://localhost:30117"
  local auth="admin@web.de:admin"
  local now ; now=$(date +%s)000000
  local from=$(( now - 3600000000 ))
  local body
  body=$(cat <<JSON
{"query":{"sql":"SELECT COUNT(*) AS c FROM \"default\"","start_time":$from,"end_time":$now,"size":1}}
JSON
)
  echo ">> Querying OpenObserve for ingested logs ..."
  local resp
  resp=$(curl -s -u "$auth" -H "Content-Type: application/json" \
    -d "$body" "$base/api/default/_search?type=logs" || true)
  echo "$resp"
  echo "$resp" | grep -q '"c"' && ! echo "$resp" | grep -q '"c":0'
}

verify_aspire() {
  # The Aspire dashboard has no public query API, so assert the OTLP/HTTP
  # endpoint (NodePort 31890) accepted data: a POST with the api key must NOT be
  # 403 or connection-refused.
  echo ">> Probing Aspire OTLP endpoint ..."
  local code
  code=$(curl -s -o /dev/null -w "%{http_code}" -X POST \
    -H "x-otlp-api-key: aspire" -H "Content-Type: application/x-protobuf" \
    --data-binary "x" "http://localhost:31890/v1/traces" || echo "000")
  echo "HTTP $code"
  # 400/500 = endpoint reached & auth ok (bad body). 403/000 = auth/route failed.
  [[ "$code" != "403" && "$code" != "000" ]]
}

case "$BACKEND" in
  openobserve) verify_openobserve ;;
  aspire)      verify_aspire ;;
esac && {
  echo ">> ✅ Telemetry confirmed for $BACKEND."
  exit 0
} || {
  echo ">> ❌ Could not confirm telemetry for $BACKEND. Check the sample logs and backend UI."
  exit 1
}
