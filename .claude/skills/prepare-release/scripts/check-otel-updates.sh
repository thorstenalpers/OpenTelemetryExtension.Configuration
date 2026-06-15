#!/usr/bin/env bash
# Decides whether there is anything worth releasing.
#
# A release is warranted when EITHER a dependency of the shipped library project
# has an update OR there are new commits since the last tag (features, fixes,
# breaking changes). Only the library project's packages matter — updates in the
# Sample or Tests projects are never published and must not drive a release.
#
# Exit 0 = something to release, 3 = nothing to release, 1 = error.
set -euo pipefail

# Run from the repository root so the relative paths below resolve regardless
# of where the script was invoked from.
cd "$(git rev-parse --show-toplevel)"

PROJ="src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj"

echo "Checking for outdated packages in $PROJ ..."
# Only count real package rows (they start with "> "); skip status lines such as
# "The given project `OpenTelemetry...` has no updates", which otherwise match.
outdated="$(dotnet list "$PROJ" package --outdated 2>/dev/null | grep -E '^[[:space:]]*>' | grep -i 'OpenTelemetry' || true)"

last_tag="$(git tag --sort=-v:refname | head -1)"
if [[ -n "$last_tag" ]]; then
  commits="$(git log "$last_tag"..HEAD --oneline 2>/dev/null || true)"
else
  commits="$(git log --oneline 2>/dev/null || true)"
fi

if [[ -z "$outdated" && -z "$commits" ]]; then
  echo "No OpenTelemetry updates and no new commits since ${last_tag:-the start} — nothing to release."
  exit 3
fi

if [[ -n "$outdated" ]]; then
  echo "OpenTelemetry updates available:"
  echo "$outdated"
fi
if [[ -n "$commits" ]]; then
  echo "Releasable changes since ${last_tag:-the start}:"
  echo "$commits"
fi
exit 0
