#!/usr/bin/env bash
# Lists outdated OpenTelemetry NuGet packages in the solution.
# Exit code 0 = updates available, 3 = nothing to release, 1 = error.
set -euo pipefail

SLN="OpenTelemetryExtension.slnx"

echo "Checking for outdated OpenTelemetry packages in $SLN ..."
outdated="$(dotnet list "$SLN" package --outdated 2>/dev/null | grep -i 'OpenTelemetry' || true)"

if [[ -z "$outdated" ]]; then
  echo "No OpenTelemetry package updates available — nothing to release."
  exit 3
fi

echo "OpenTelemetry updates available:"
echo "$outdated"
exit 0
