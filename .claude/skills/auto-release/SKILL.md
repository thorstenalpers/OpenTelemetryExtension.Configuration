---
name: auto-release
description: Prepare a new NuGet release of this repository. Use when the user wants to cut/prepare a release, bump the package version, update dependencies for a release, or open a release PR to master. Decides the next SemVer version automatically and only releases when OpenTelemetry packages have updates.
---

# Auto-release

Prepare a new release of this NuGet package. You decide the next version
yourself. The NuGet publish itself is a **manual** GitHub Actions trigger
(`deploy-nuget.yml`, `workflow_dispatch`) and is NOT part of your job — you
prepare the repository and open the PR.

## Workflow

1. **Check for OpenTelemetry NuGet updates first**
   - Run the helper: `bash scripts/check-otel-updates.sh`
   - Exit code **3** = no OpenTelemetry updates → **stop and do not release**.
   - Exit code **0** = updates available → continue.

2. **Determine current version & last tag**
   - Read `<Version>` in `src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`.
   - Latest tag: `git tag --sort=-v:refname | head -1`.

3. **Decide the next version (SemVer)** from `git log --oneline <tag>..HEAD`:
   - **MAJOR** breaking API changes · **MINOR** new features · **PATCH** fixes/docs/deps.
   - Must be strictly greater than the current version. State it + a one-line reason.

4. **Confirm clean tree** — `git status --short`; if dirty, ask how to proceed.

5. **Branch** — `git checkout -b release/v<version>`.

6. **Update all NuGet packages** to latest, then `dotnet restore OpenTelemetryExtension.slnx`.

7. **Build & test (green required)**
   - `dotnet build OpenTelemetryExtension.slnx -c Release`
   - `dotnet test OpenTelemetryExtension.slnx -c Release`

8. **End-to-end smoke test** — prove telemetry actually reaches a backend.
   Requires a local Kubernetes cluster (k3s in WSL2) with Helm + kubectl. The
   helper starts the backend via its Helm chart, runs the sample with the
   matching launch profile, generates traffic and verifies ingestion:
   - `bash scripts/smoke-test.sh openobserve`  (queries the OpenObserve API for records)
   - `bash scripts/smoke-test.sh aspire`       (probes the Aspire OTLP endpoint)
   - Exit 0 = telemetry confirmed → continue. Non-zero = stop and report; do not
     release if telemetry does not arrive.

9. **Bump `<Version>`** in the csproj.

10. **Extend docs** — update `README.md` etc. for the changes/new dep versions.

11. **Release notes** — copy `templates/release-notes.md` to
    `release-notes/v<version>.md`, fill in the `{{VERSION}}`/`{{DATE}}` placeholders
    and the **Added / Changed / Fixed / Removed** sections (omit empty ones).

12. **Commit** (no tag) — stage csproj(s), docs, release notes; message `release: v<version>`.

13. **Push & PR to master**
    - `git push -u origin release/v<version>`
    - `gh pr create --base master --head release/v<version> --title "release: v<version>" --body <notes>`

14. **Report** the PR link and remind: after merge, trigger **Deploy Nuget** manually
    (Actions → Deploy Nuget → Run workflow) — it tags `v<version>` and publishes.
