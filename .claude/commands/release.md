---
description: Prepare a new release — bump version, write release notes, commit
argument-hint: <new-version>  (e.g. 1.2.0)
allowed-tools: Bash(git*), Read, Edit, Write, Grep, Glob
---

# Prepare release v$1

You are preparing a new release of this NuGet package. The NuGet publish itself
is a **manual** GitHub Actions trigger (`deploy-nuget.yml`, `workflow_dispatch`)
and is NOT part of your job — you only prepare the repository.

Target version: **$1** (if empty, ask the user which version to release).

## Steps

1. **Validate the version argument**
   - Must be valid SemVer (`MAJOR.MINOR.PATCH`).
   - Read the current `<Version>` in
     `src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`.
   - The new version must be strictly greater than the current one. If not, stop
     and tell the user.

2. **Confirm a clean tree**
   - Run `git status --short`. If there are uncommitted changes, stop and ask the
     user how to proceed (the release commit should be isolated).

3. **Bump the version**
   - Edit `<Version>` in the csproj to `$1`.

4. **Write release notes**
   - Create `release-notes/v$1.md`.
   - Summarise changes since the previous release tag. Get them with:
     `git log --oneline <previous-tag>..HEAD` (find the latest `v*` tag via
     `git tag --sort=-v:refname | head -1`).
   - Group into sections: **Added**, **Changed**, **Fixed**, **Removed**
     (omit empty sections). Write user-facing notes, not raw commit subjects.

5. **Run the test suite** to make sure the release is green:
   `dotnet test src/OpenTelemetryExtension.slnx -c Release --filter "Category!=Long-Running"`
   - If tests fail, stop and report — do not commit a broken release.

6. **Commit** (do NOT tag — the workflow creates the tag):
   - Stage only the csproj and the new release-notes file.
   - Commit message: `release: v$1`

7. **Report** what you did and remind the user of the final manual step:
   > Push the commit, then trigger the **Deploy Nuget** workflow manually
   > (Actions → Deploy Nuget → Run workflow). It reads `<Version>` from the csproj
   > and `release-notes/v$1.md`, publishes to NuGet + GitHub Packages, and creates
   > the `v$1` tag and GitHub Release.

Do not push automatically unless the user asks.
