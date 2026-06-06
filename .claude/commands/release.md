---
description: Prepare a new release — update NuGets, test, docs, release notes, bump version, commit, push, open PR to master
argument-hint: <new-version>  (e.g. 1.2.0)
allowed-tools: Bash, Read, Edit, Write, Grep, Glob
---

# Prepare release v$1

You are preparing a new release of this NuGet package. The NuGet publish itself
is a **manual** GitHub Actions trigger (`deploy-nuget.yml`, `workflow_dispatch`)
and is NOT part of your job — you prepare the repository and open the PR.

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

3. **Create a release branch**
   - Branch off the current branch: `git checkout -b release/v$1`.

4. **Update all NuGet packages to their latest versions**
   - For every project, list outdated packages:
     `dotnet list OpenTelemetryExtension.slnx package --outdated`
   - Update each outdated package to the latest version, e.g.
     `dotnet add <project> package <PackageName>` (no version pins the latest),
     or bump the `<PackageReference>` `Version` attributes directly in the csproj files.
   - Restore and confirm there are no version conflicts:
     `dotnet restore OpenTelemetryExtension.slnx`

5. **Build & run the full test suite** — the release must be green:
   - `dotnet build OpenTelemetryExtension.slnx -c Release`
   - `dotnet test OpenTelemetryExtension.slnx -c Release`
   - If the build or any test fails, stop and report — do not continue with a
     broken release.

6. **Bump the version**
   - Edit `<Version>` in the csproj to `$1`.

7. **Extend the documentation**
   - Review and update `README.md` (and any other docs) so they reflect the
     package changes since the last release — new/changed options, updated
     dependency versions, new examples, etc.

8. **Write release notes**
   - Create `release-notes/v$1.md`.
   - Summarise changes since the previous release tag. Get them with:
     `git log --oneline <previous-tag>..HEAD` (find the latest `v*` tag via
     `git tag --sort=-v:refname | head -1`).
   - Group into sections: **Added**, **Changed**, **Fixed**, **Removed**
     (omit empty sections). Write user-facing notes, not raw commit subjects.
   - Include a note that dependencies were updated to their latest versions.

9. **Commit** (do NOT tag — the workflow creates the tag):
   - Stage the csproj(s), updated docs, and the new release-notes file.
   - Commit message: `release: v$1`

10. **Push and open a PR to master**
    - `git push -u origin release/v$1`
    - Open a pull request targeting `master` with the `gh` CLI:
      `gh pr create --base master --head release/v$1 --title "release: v$1" --body <release notes summary>`
    - Use the contents of `release-notes/v$1.md` for the PR body.

11. **Report** what you did, link the PR, and remind the user of the final manual step:
    > After the PR is merged to master, trigger the **Deploy Nuget** workflow
    > manually (Actions → Deploy Nuget → Run workflow). It reads `<Version>` from
    > the csproj and `release-notes/v$1.md`, publishes to NuGet + GitHub Packages,
    > and creates the `v$1` tag and GitHub Release.
