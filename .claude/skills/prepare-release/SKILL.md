---
name: prepare-release
description: Prepare a new NuGet release of this repository (version bump, release notes, release PR to main). Use this whenever the user mentions releasing, publishing, shipping, cutting a release, bumping the version, updating OpenTelemetry dependencies, or preparing release notes — even if they don't say "release" explicitly. Decides the next SemVer version automatically and proceeds when the shipped library project has dependency updates or there are any new commits since the last tag.
---

# Prepare release

Prepare a new release of this NuGet package. You decide the next version
yourself. The NuGet publish itself is a **manual** GitHub Actions trigger
(`deploy-nuget.yml`, `workflow_dispatch`) and is NOT part of your job — you
prepare the repository and open the PR.

## Workflow

1. **Check whether there is anything to release**
   - Run the helper: `bash .claude/skills/prepare-release/scripts/check-otel-updates.sh`
   - The helper only inspects the **shipped library project** (`src/OpenTelemetryExtension.Configuration/...csproj`). Dependency updates in the Sample or Tests projects are ignored — they are never published and must not trigger a new version.
   - Exit code **3** = nothing to release (no library dependency updates *and* no new commits since the last tag) → **stop**.
   - Exit code **0** = library dependency updates and/or new commits exist → continue.

2. **Determine current version & last tag**
   - Read `<Version>` in `src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`.
   - Latest tag: `git tag --sort=-v:refname | head -1`.

3. **Decide the next version (SemVer)** from `git log --oneline <tag>..HEAD`:
   - **MAJOR** breaking API changes · **MINOR** new features · **PATCH** fixes/docs/deps.
   - Must be strictly greater than the current version. State it + a one-line reason.

4. **Confirm clean tree** — `git status --short`; if dirty, ask how to proceed.

5. **Branch** — `git checkout -b release/v<version>`.

6. **Update the library project's NuGet packages** to latest (`src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj` only — leave Sample/Tests packages alone), then `dotnet restore OpenTelemetryExtension.slnx`.

7. **Build & test (green required)**
   - `dotnet build OpenTelemetryExtension.slnx -c Release`
   - `dotnet test OpenTelemetryExtension.slnx -c Release --filter "Category=Unit"` (unit tests only; integration tests need the live OpenObserve/SQL stack)

8. **End-to-end smoke test** — prove telemetry actually reaches a backend.
   Requires a local Kubernetes cluster (k3s in WSL2) with Helm + kubectl.
   OpenObserve is used because it has a real query API, so the test can
   positively confirm ingested data. The helper starts OpenObserve via its Helm
   chart, runs the sample, generates traffic and queries the API for records:
   - `bash .claude/skills/prepare-release/scripts/smoke-test.sh`
   - Exit 0 = telemetry confirmed → continue. Non-zero = stop and report; do not
     release if telemetry does not arrive.

9. **Bump `<Version>`** in the csproj.

10. **Extend docs** — update `README.md` etc. for the changes/new dep versions.

11. **Release notes** — copy `.claude/skills/prepare-release/assets/release-notes.md` to
    `release-notes/v<version>.md`, fill in the `{{VERSION}}`/`{{DATE}}` placeholders
    and the **Added / Changed / Fixed / Removed** sections (omit empty ones).

12. **Commit** (no tag) — stage csproj(s), docs, release notes; message `release: v<version>`.

13. **Push & PR to main**
    - `git push -u origin release/v<version>`
    - `gh pr create --base main --head release/v<version> --title "release: v<version>" --body <notes>`

14. **Report** the PR link and remind: after merge, trigger **Deploy Nuget** manually
    (Actions → Deploy Nuget → Run workflow) — it tags `v<version>` and publishes.
