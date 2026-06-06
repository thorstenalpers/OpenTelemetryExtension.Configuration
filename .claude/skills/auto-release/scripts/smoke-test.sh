#!/usr/bin/env bash
# Smoke-test: start OpenObserve via Helm, run the sample, generate traffic and
# verify that telemetry actually arrived by querying the OpenObserve API.
#
# OpenObserve is used because it exposes a real query API, so we can positively
# prove that data was ingested (not just that the endpoint was reachable).
#
# Usage: bash smoke-test.sh
#
# Requires a local Kubernetes cluster (e.g. k3s in WSL2) with Helm + kubectl.
# Exit 0 = telemetry confirmed, non-zero = failed.
set -euo pipefail

SAMPLE_DIR="src/OpenTelemetryExtension.Configuration.Sample"
SWAGGER_URL="http://localhost:5021"   # http app url from launchSettings

# 1. Start OpenObserve via its Helm chart
echo ">> Starting OpenObserve via Helm ..."
(cd infrastructure/helm && helm upgrade --install openobserve ./chart-openobserve)
echo ">> Waiting for backend to become ready ..."
sleep 20

# 2. Run the sample in the background with the OpenObserve HTTP profile
echo ">> Running sample with profile: Start OpenObserve Http"
( cd "$SAMPLE_DIR" && dotnet run --launch-profile "Start OpenObserve Http" ) &
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

# 4. Give the exporter time to flush, then verify ingestion via the query API.
#    NodePort 30117. The _search API needs the user password (rootPassword: admin),
#    not the OTLP ingestion passcode.
echo ">> Waiting for export/flush ..."
sleep 15

BASE="http://localhost:30117"
AUTH="admin@web.de:admin"
NOW=$(date +%s)000000
FROM=$(( NOW - 3600000000 ))
BODY=$(cat <<JSON
{"query":{"sql":"SELECT COUNT(*) AS c FROM \"default\"","start_time":$FROM,"end_time":$NOW,"size":1}}
JSON
)

echo ">> Querying OpenObserve for ingested logs ..."
RESP=$(curl -s -u "$AUTH" -H "Content-Type: application/json" \
  -d "$BODY" "$BASE/api/default/_search?type=logs" || true)
echo "$RESP"

if echo "$RESP" | grep -q '"c"' && ! echo "$RESP" | grep -q '"c":0'; then
  echo ">> ✅ Telemetry confirmed in OpenObserve."
  exit 0
else
  echo ">> ❌ Could not confirm telemetry. Check the sample logs and the OpenObserve UI (http://localhost:30117)."
  exit 1
fi
