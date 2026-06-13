# Contributing

Contributions are welcome — bug fixes, improvements, and ideas alike.

## Before You Start

For anything beyond a small fix, please [open an issue](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/issues/new/choose) first so we can align before you invest time in a pull request.

## Workflow

1. Fork the repository and create a branch from `main` — use `feature/<name>`
   for new functionality or `fix/<name>` for bug fixes.
2. Make your changes.
3. Add or update tests — every public API change needs test coverage.
4. When you add or change a feature, run the **unit tests**; if you have the
   telemetry stack running, run the **integration tests** too (see
   [Integration tests](#integration-tests)).
5. Open a pull request against `main`.

## Build & Test

```bash
dotnet build OpenTelemetryExtension.slnx -c Release
dotnet test  src/OpenTelemetryExtension.Configuration.Tests -c Release   # unit tests
```

### Integration tests

The separate `OpenTelemetryExtension.Configuration.IntegrationTests` project
verifies that telemetry is actually exported: it sends logs, metrics, traces and
a SQL Server span through `AddTelemetry()` to a live **OpenObserve** instance and
queries its API to confirm the data arrived.

```bash
# 1. Start the backends (local Kubernetes + Helm required)
infrastructure/helm/helm-install-openobserve.cmd
infrastructure/helm/helm-install-sqlserver.cmd   # only needed for the SQL Server test

# 2. Run the integration tests
dotnet test src/OpenTelemetryExtension.Configuration.IntegrationTests -c Release
```

The tests are tagged `[Trait("Category", "Integration")]` and **skip
automatically** when OpenObserve (or SQL Server) is unreachable, so a normal
`dotnet test` run never fails just because the stack is down. Endpoints and
credentials default to the Helm chart values and can be overridden via the
`OTEL_IT_*` environment variables. **CI runs the unit tests only.**

## Code Style

- File-scoped namespaces (`namespace Foo;`)
- Nullable reference types enabled — no `!` without a reason
- Private fields: `_camelCase`, static fields: `s_camelCase`
- No comments explaining *what* the code does — only *why* (hidden constraints, workarounds)
- EditorConfig is enforced at build time — run the build to catch style violations

## Versioning

This project uses [Semantic Versioning](https://semver.org).

**As a contributor, do not touch the version or release notes.** Leave the
`<Version>` property in
[`OpenTelemetryExtension.Configuration.csproj`](./src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj)
and the `release-notes/` folder alone. Bumping the version inside a feature PR
causes merge conflicts when several PRs are in flight (two PRs editing the same
`<Version>` line or claiming the same `release-notes/vX.Y.Z.md` file), so
versioning is deliberately a separate, release-time step — not part of feature
work.

Instead, just describe your change clearly in the PR and flag any breaking
change. At release time the maintainer cuts the version and writes the notes;
the `prepare-release` skill decides the SemVer bump (PATCH / MINOR / MAJOR) from
the commits merged since the last release.

> **Maintainers:** the release-prep steps (version decision, dependency updates, build/test, smoke test, release notes, release PR) are automated by the `prepare-release` Claude Code skill in [`.agents/skills/prepare-release/`](./.agents/skills/prepare-release/). It prepares the PR only — the actual NuGet publish remains the manual **Deploy Nuget** workflow.

## Adding a New Instrumentation Option

Follow the checklist in [CLAUDE.md](./CLAUDE.md#adding-a-new-instrumentation-option).

## Licensing

By submitting a pull request you agree that your contribution will be licensed under the [MIT License](./LICENSE).
