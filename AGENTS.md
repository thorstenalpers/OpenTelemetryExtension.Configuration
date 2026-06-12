# AGENTS.md

Canonical instructions for AI coding agents working in this repository.
Tool-specific files (`CLAUDE.md`, `.github/copilot-instructions.md`) point here.

## Project

NuGet package that wires up OpenTelemetry (tracing, metrics, logging) for
.NET applications via a single `AddTelemetry()` call and `appsettings.json`.

## Repository layout

```
src/
  Directory.Build.props                            # Shared props: nullable, implicit usings, warnings-as-errors, style enforced in build
  OpenTelemetryExtension.Configuration/             # Library (netstandard2.0 + net8.0 + net10.0) — the shipped NuGet package
  OpenTelemetryExtension.Configuration.Tests/       # xUnit unit tests (net10.0, in-process, Category=Unit)
  OpenTelemetryExtension.Configuration.IntegrationTests/  # Integration tests (net10.0, Category=Integration) — query a live OpenObserve
  OpenTelemetryExtension.Configuration.Sample.WebApi/  # ASP.NET Core sample app (net10.0)
  OpenTelemetryExtension.Configuration.Sample.Wpf/     # WPF desktop sample app (net10.0-windows)
infrastructure/helm/                             # Helm charts + install scripts (OpenObserve, SQL Server, Aspire Dashboard, SigNoz)
.github/workflows/
  ci.yml                                         # Build + unit tests (Category=Unit) + coverage → Coveralls
  deploy-nuget.yml                               # Manual NuGet publish + git tag + GitHub Release
release-notes/                                   # v{VERSION}.md per release
OpenTelemetryExtension.slnx                      # Solution file (repo root)
```

`OpenTelemetryExtension.slnx` lists projects **and** loose files (docs, helm
charts, release notes) explicitly. Whenever you **add, rename, move, or delete**
a tracked file — including a new `release-notes/v{VERSION}.md` — update the
`.slnx` to match, or the solution will reference a missing path or miss the new
file.

## Branches & CI

The repository follows **GitHub Flow**: `main` is the only long-lived branch and
is always releasable.

- Branch off `main` for every change — `feature/*` for new functionality,
  `fix/*` for bug fixes — then open a PR back to `main`. Do not commit directly
  to `main`.
- `release/vX.Y.Z` branches are the same mechanism but carry **only** release
  mechanics (version bump, dependency updates, release notes) — no code changes.
  They are created by the `prepare-release` skill, not by contributors.
- `ci.yml` runs on push to `main` and on every PR targeting `main` (Windows
  runner): build, unit tests with `--filter "Category=Unit"`, coverage via
  Coverlet + ReportGenerator, upload to Coveralls.
- `deploy-nuget.yml` is **manual only** (`workflow_dispatch`, Linux runner): it
  builds *only* the library and unit-test projects (the WPF sample cannot build
  on Linux), runs the unit tests, packs, publishes to NuGet.org and GitHub
  Packages, tags `v{VERSION}` and creates a GitHub Release from
  `release-notes/v{VERSION}.md`.

## Roles & permissions (admin-only actions)

`main` is protected (PR required, `build` check must pass, `enforce_admins` on —
**nobody pushes to `main` directly, including the admin**). On top of that, the
following are reserved for the repository **maintainer (admin)** and an AI agent
must **never** do them on its own initiative:

- **Reviewing & merging PRs into `main`.** The admin manually reviews **every**
  PR and performs the merge. An agent prepares branches and **opens** PRs, then
  **stops** — it does not merge them, does not enable auto-merge, and does not
  merge its own PRs.
- **Opening PRs automatically.** An agent may open a PR only while explicitly
  operated by the admin (e.g. the admin running the `prepare-release` skill).
- **Triggering `deploy-nuget.yml`.** Publishing (NuGet + GitHub Packages + the
  `v{VERSION}` tag + GitHub Release) is a manual, admin-only `workflow_dispatch`,
  run **after** the admin has merged the release PR. An agent never triggers it.

## Public API (2 classes, minimal surface)

```csharp
// IServiceCollection extensions
services.AddTelemetry(configuration);                       // binds "Telemetry" section
services.AddTelemetry(configuration, "CustomSection");      // custom section name
services.AddTelemetry(configuration, o => { ... });         // bind + code callback (combined)
services.AddTelemetry(configuration, o => { ... }, "Sec");  // combined + custom section
services.AddTelemetry(o => { o.Endpoint = new Uri("..."); });
```

`TelemetryOptions` is the single configuration model:

- **Connection**: `Endpoint` (`[Required]`, validated at registration when
  enabled), `Headers`, `Protocol` (default `HttpProtobuf`)
- **Identity**: `ServiceName`, `ResourceAttributes`
- **Signal switches**: `Enabled` (default `true`; `false` makes `AddTelemetry()`
  a no-op), `EnableTracing`, `EnableMetrics`, `EnableLogging`
- **Instrumentation switches** (all default `true`):
  `EnableAspNetCoreInstrumentation`, `EnableHttpClientInstrumentation`,
  `EnableRuntimeInstrumentation`, plus `RecordExceptions`, `ExcludedPaths`
  (default `["/health"]`)
- **Tracing/metrics extension points**: `AdditionalTracingSources`,
  `AdditionalMeters`, `SampleRatio`, and `ConfigureTracing` /
  `ConfigureMetrics` / `ConfigureLogging` callbacks
- **Logging**: `IncludeScopes`, `IncludeFormattedMessage`

The configuration section name (`Telemetry`) is overridable via the
`sectionName` parameter on the `IConfiguration` overloads.

## Dependencies

- Main library: only OpenTelemetry SDK packages
  (`OpenTelemetry.Exporter.OpenTelemetryProtocol`,
  `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.Http`,
  `OpenTelemetry.Instrumentation.Runtime`;
  `OpenTelemetry.Instrumentation.AspNetCore` is conditional — **not**
  referenced for `netstandard2.0` so non-web clients stay lean). Do not add
  other third-party packages.
- Unit tests: xUnit + coverlet. The library has `InternalsVisibleTo`
  for the unit-test project.

## Build & test

```bash
dotnet build OpenTelemetryExtension.slnx -c Release
dotnet test  src/OpenTelemetryExtension.Configuration.Tests -c Release --filter "Category=Unit"   # unit tests
```

Unit tests run in-process via `ServiceCollection` — no infrastructure required.

**Whenever you add or change a feature, run the unit tests. When the telemetry
stack is running, also run the integration tests** (see below). **CI runs the
unit tests only.**

### Integration tests

`OpenTelemetryExtension.Configuration.IntegrationTests` exercises the real export
path: it emits logs, metrics and traces (and a SQL Server span) through
`AddTelemetry()` to a running **OpenObserve** instance and queries its `_search`
API to confirm the data was ingested.

- Needs the OpenObserve Helm chart (`infrastructure/helm/helm-install-openobserve.cmd`);
  the SQL Server chart (`helm-install-sqlserver.cmd`) is required only for the SQL test.
- Tests use `[IntegrationFact]` / `[SqlIntegrationFact]` (in `Utils/`) instead
  of `[Fact]` — they **auto-skip** when OpenObserve (`localhost:30117`) or SQL
  Server (`localhost:31433`) is unreachable, so the suite stays green without
  the stack.
- Shared helpers live in `Utils/`: `IntegrationConfig` (endpoints/credentials),
  `OpenObserveClient` (`_search` queries), `OtelTestHost`, `Reachability`.
- Endpoints/credentials default to the Helm chart values; override via env vars:
  `OTEL_IT_OPENOBSERVE_URL`, `OTEL_IT_OPENOBSERVE_USER`,
  `OTEL_IT_OPENOBSERVE_PASSWORD`, `OTEL_IT_OTLP_HEADERS`, `OTEL_IT_SQL_CONNECTION`.
- Run: `dotnet test src/OpenTelemetryExtension.Configuration.IntegrationTests -c Release`.

## Language & framework

- C# with nullable reference types enabled — never use `!` to suppress
  nullability without a comment explaining why
- Target frameworks: `netstandard2.0`, `net8.0` and `net10.0` — guard net5.0+
  APIs with `#if NET5_0_OR_GREATER` (or `#if !NETSTANDARD2_0`). Do not use APIs
  unavailable on `netstandard2.0` without the guard. The ASP.NET Core
  instrumentation is referenced for every target except `netstandard2.0`, so
  `net8.0` and `net10.0` consumers get it; `netstandard2.0` (WPF/console) stays
  lean.
- `src/Directory.Build.props` applies to every project: `Nullable`,
  `ImplicitUsings`, `LangVersion=latest`, `TreatWarningsAsErrors=true`,
  `EnforceCodeStyleInBuild=true` — a style violation fails the build

## Code conventions

- **File-scoped namespaces** required (`namespace Foo;` not `namespace Foo { }`)
- **EditorConfig** is enforced at build time — do not bypass it
- Private fields: `_camelCase`, static fields: `s_camelCase`, interfaces: `IFoo`,
  type params: `TFoo`
- `var` for local variables when the type is obvious from the right-hand side
- Expression-bodied members for single-line methods/properties
- Pattern matching over `is`/`as` + cast
- No comments explaining *what* code does — only *why* (hidden constraints,
  workarounds)
- No trailing XML doc blocks on self-explanatory members

## Tests

- **Every unit test class must carry `[Trait("Category", "Unit")]`** (class
  level). CI and the deploy workflow filter on `Category=Unit` — an untagged
  test is silently never run in CI.
- Integration test classes carry `[Trait("Category", "Integration")]` and use
  `[IntegrationFact]` / `[SqlIntegrationFact]` instead of `[Fact]`
- xUnit `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterised
- Method name pattern: `MethodOrProperty_Condition_ExpectedResult`
- Arrange / Act / Assert with a blank line between each section; trivial
  single-expression tests (e.g. default-value checks) may stay compact
- Use `ServiceCollection` + `BuildServiceProvider()` to verify DI registrations —
  no reflection hacks
- Use `Record.Exception` (not `Assert.Throws<T>`) when asserting that no
  exception is thrown
- Do not use `Thread.Sleep` or `Task.Delay` in **unit** tests (integration tests
  may poll the backend until telemetry is queryable)

## Versioning & release

- Version lives in
  `src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`
  (`<Version>`)
- Do not change `<Version>` without also creating `release-notes/v{VERSION}.md`
  (and adding it to `OpenTelemetryExtension.slnx`) — the GitHub Release body is
  taken from that file
- NuGet publish is **manual** (`workflow_dispatch` on `deploy-nuget.yml`) —
  never triggered automatically; it also creates the `v{VERSION}` git tag. Only
  the admin triggers it, **after** merging the release PR (see
  [Roles & permissions](#roles--permissions-admin-only-actions))
- The full release-prep workflow (decide SemVer, bump, update deps, build/test,
  end-to-end smoke test, release notes, PR to `main`) is encoded in the
  **`prepare-release`** skill at `.claude/skills/prepare-release/`. Run it via
  Claude Code (`/prepare-release`) when cutting a release; it only prepares and
  **opens** the PR — the admin reviews and merges it, then the admin triggers
  `deploy-nuget.yml`. The skill never merges or publishes.

## What NOT to do

- Do not add `using` directives already covered by global/implicit usings
- Do not add `// TODO` comments — raise an issue instead
- Do not modify the `*.Sample.*` projects for library behaviour changes (they
  are excluded from code coverage)
- Do not add new public API surface without a corresponding test in
  `TelemetryOptionsTests.cs` or `TelemetryServiceCollectionExtensionsTests.cs`
- Do not add test classes without a `Category` trait (see [Tests](#tests))
- Do not merge PRs into `main`, enable auto-merge, or trigger `deploy-nuget.yml`
  — those are admin-only (see [Roles & permissions](#roles--permissions-admin-only-actions))
- Do not add, rename, or delete a tracked file without updating
  `OpenTelemetryExtension.slnx`

## Adding a new instrumentation option

1. Add `bool EnableXxxInstrumentation { get; set; } = true;` to `TelemetryOptions`
   with XML doc + Default comment
2. Wire it up in `TelemetryServiceCollectionExtensions` under the appropriate
   signal block
3. Add default-value test in `TelemetryOptionsTests.cs`
4. Add enabled/disabled unit tests in
   `TelemetryServiceCollectionExtensionsTests.cs`
5. Add the option to the `<example>` block in the XML doc on `TelemetryOptions`
6. Update `README.md` configuration reference table
7. Run the unit tests; when the telemetry stack is running, run the integration
   tests too (see [Integration tests](#integration-tests))
