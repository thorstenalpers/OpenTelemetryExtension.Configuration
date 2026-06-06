---
description: Auto-release — decide next version, update NuGets, test, docs, release notes, bump version, commit, push, open PR to master
allowed-tools: Bash, Read, Edit, Write, Grep, Glob
---

# Auto-release

You are preparing a new release of this NuGet package. You decide the next
version yourself. The NuGet publish itself is a **manual** GitHub Actions trigger
(`deploy-nuget.yml`, `workflow_dispatch`) and is NOT part of your job — you
prepare the repository and open the PR.

## Steps

1. **Check for OpenTelemetry NuGet updates first**
   - List outdated packages:
     `dotnet list OpenTelemetryExtension.slnx package --outdated`
   - If **no OpenTelemetry packages** (any `OpenTelemetry*` package reference) are
     outdated, **stop here and do not create a release** — report that there are
     no OpenTelemetry updates and nothing to release.
   - Only continue when at least one OpenTelemetry package has an update available.

2. **Determine the current version**
   - Read the current `<Version>` in
     `src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`.
   - Find the latest release tag: `git tag --sort=-v:refname | head -1`.

3. **Decide the next version automatically (SemVer)**
   - Inspect the commits since the last tag:
     `git log --oneline <previous-tag>..HEAD`.
   - Choose the bump based on the nature of the changes:
     - **MAJOR** — breaking API changes (removed/renamed public members, changed
       behavior that breaks callers).
     - **MINOR** — new backwards-compatible features or options.
     - **PATCH** — bug fixes, docs, dependency updates, internal changes only.
   - The new version must be strictly greater than the current `<Version>`.
   - State the chosen version and a one-line justification before continuing.

4. **Confirm a clean tree**
   - Run `git status --short`. If there are uncommitted changes, stop and ask the
     user how to proceed (the release commit should be isolated).

5. **Create a release branch**
   - Branch off the current branch: `git checkout -b release/v<version>`.

6. **Update all NuGet packages to their latest versions**
   - Update each outdated package to the latest version, e.g.
     `dotnet add <project> package <PackageName>` (no version pins the latest),
     or bump the `<PackageReference>` `Version` attributes directly in the csproj files.
   - Restore and confirm there are no version conflicts:
     `dotnet restore OpenTelemetryExtension.slnx`

7. **Build & run the full test suite** — the release must be green:
   - `dotnet build OpenTelemetryExtension.slnx -c Release`
   - `dotnet test OpenTelemetryExtension.slnx -c Release`
   - If the build or any test fails, stop and report — do not continue with a
     broken release.

8. **Bump the version**
   - Edit `<Version>` in the csproj to the chosen version.

9. **Extend the documentation**
   - Review and update `README.md` (and any other docs) so they reflect the
     package changes since the last release — new/changed options, updated
     dependency versions, new examples, etc.

10. **Write release notes**
    - Create `release-notes/v<version>.md`.
    - Group the changes into sections: **Added**, **Changed**, **Fixed**,
      **Removed** (omit empty sections). Write user-facing notes, not raw commit
      subjects.
    - Include a note that dependencies were updated to their latest versions.

11. **Commit** (do NOT tag — the workflow creates the tag):
    - Stage the csproj(s), updated docs, and the new release-notes file.
    - Commit message: `release: v<version>`

12. **Push and open a PR to master**
    - `git push -u origin release/v<version>`
    - Open a pull request targeting `master` with the `gh` CLI:
      `gh pr create --base master --head release/v<version> --title "release: v<version>" --body <release notes summary>`
    - Use the contents of `release-notes/v<version>.md` for the PR body.

13. **Report** what you did, link the PR, and remind the user of the final manual step:
    > After the PR is merged to master, trigger the **Deploy Nuget** workflow
    > manually (Actions → Deploy Nuget → Run workflow). It reads `<Version>` from
    > the csproj and `release-notes/v<version>.md`, publishes to NuGet + GitHub
    > Packages, and creates the `v<version>` tag and GitHub Release.
