# AGENTS.md

Canonical instructions for AI coding agents working in this repository.
Tool-specific files (`CLAUDE.md`, `.github/copilot-instructions.md`) point here.

## Project

NuGet package that wires up OpenTelemetry (tracing, metrics, logging) for
ASP.NET Core via a single `AddTelemetry()` call and `appsettings.json`.

## Repository layout

```
src/
  OpenTelemetryExtension.Configuration/             # Library (netstandard2.0 + net10.0)
  OpenTelemetryExtension.Configuration.Tests/       # xUnit unit tests (net10.0, in-process)
  OpenTelemetryExtension.Configuration.IntegrationTests/  # Integration tests (net10.0) — query a live OpenObserve
  OpenTelemetryExtension.Configuration.Sample.WebApi/  # ASP.NET Core sample app (net10.0)
  OpenTelemetryExtension.Configuration.Sample.Wpf/     # WPF desktop sample app (net10.0-windows)
  OpenTelemetryExtension.slnx                    # Solution file
.github/workflows/
  ci.yml                                         # Build + test + coverage on push
  deploy-nuget.yml                               # Manual NuGet publish + GitHub Release
release-notes/                                   # v{VERSION}.md per release
```

## Public API (2 classes, minimal surface)

```csharp
// IServiceCollection extensions
services.AddTelemetry(configuration);                       // binds "Telemetry" section
services.AddTelemetry(configuration, "CustomSection");      // custom section name
services.AddTelemetry(configuration, o => { ... });         // bind + code callback (combined)
services.AddTelemetry(configuration, o => { ... }, "Sec");  // combined + custom section
services.AddTelemetry(o => { o.Endpoint = new Uri("..."); });
```

`TelemetryOptions` is the single configuration model. `Enabled` defaults to
`true`; set it to `false` to make `AddTelemetry()` a no-op. `Endpoint` is
`[Required]` and validated at registration time when `Enabled = true`. The
configuration section name (`Telemetry`) is overridable via the `sectionName`
parameter on the `IConfiguration` overloads.

## Build & test

```bash
dotnet build OpenTelemetryExtension.slnx -c Release
dotnet test  src/OpenTelemetryExtension.Configuration.Tests -c Release   # unit tests
```

Unit tests use **xUnit + Moq** and run in-process via `ServiceCollection` — no
infrastructure required.

**Whenever you add or change a feature, run the unit tests. When the telemetry
stack is running, also run the integration tests** (see below). **CI runs the
unit tests only** (the workflows filter on `Category=Unit`).

### Integration tests

`OpenTelemetryExtension.Configuration.IntegrationTests` exercises the real export
path: it emits logs, metrics and traces (and a SQL Server span) through
`AddTelemetry()` to a running **OpenObserve** instance and queries its `_search`
API to confirm the data was ingested.

- Needs the OpenObserve Helm chart (`infrastructure/helm/helm-install-openobserve.cmd`);
  the SQL Server chart (`helm-install-sqlserver.cmd`) is required only for the SQL test.
- Every test is `[Trait("Category", "Integration")]` and **auto-skips** when the
  backend (or SQL Server) is unreachable, so the suite stays green without the stack.
- Endpoints/credentials default to the Helm chart values; override via `OTEL_IT_*` env vars.
- Run: `dotnet test src/OpenTelemetryExtension.Configuration.IntegrationTests -c Release`.

## Language & framework

- C# with nullable reference types enabled — never use `!` to suppress
  nullability without a comment explaining why
- Target frameworks: `netstandard2.0` and `net10.0` — guard net5.0+ APIs with
  `#if NET5_0_OR_GREATER`. Do not use APIs unavailable on `netstandard2.0`
  without the guard.
- No third-party packages in the main library beyond the OpenTelemetry SDK
  packages already referenced

## Code conventions

- **File-scoped namespaces** required (`namespace Foo;` not `namespace Foo { }`)
- **EditorConfig** is enforced at build time (`EnforceCodeStyleInBuild=True`) —
  do not bypass it
- Private fields: `_camelCase`, static fields: `s_camelCase`, interfaces: `IFoo`,
  type params: `TFoo`
- `var` for local variables when the type is obvious from the right-hand side
- Expression-bodied members for single-line methods/properties
- Pattern matching over `is`/`as` + cast
- No comments explaining *what* code does — only *why* (hidden constraints,
  workarounds)
- No trailing XML doc blocks on self-explanatory members

## Tests

- xUnit `[Fact]` for single cases, `[Theory]` + `[InlineData]` for parameterised
- Method name pattern: `MethodOrProperty_Condition_ExpectedResult`
- Arrange / Act / Assert with a blank line between each section
- Use `ServiceCollection` + `BuildServiceProvider()` to verify DI registrations —
  no reflection hacks
- Use `Record.Exception` (not `Assert.Throws<T>`) when asserting that no
  exception is thrown
- Do not use `Thread.Sleep` or `Task.Delay` in **unit** tests (integration tests
  may poll the backend until telemetry is queryable)
- Integration tests live in the `*.IntegrationTests` project, are marked
  `[Trait("Category", "Integration")]` and assert against a live OpenObserve via
  its `_search` API — see [Integration tests](#integration-tests)

## Versioning & release

- Version lives in
  `src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`
  (`<Version>`)
- Do not change `<Version>` without also creating `release-notes/v{VERSION}.md`
- NuGet publish is **manual** (`workflow_dispatch`) — never triggered
  automatically
- The full release-prep workflow (decide SemVer, bump, update deps, build/test,
  end-to-end smoke test, release notes, PR to `master`) is encoded in the
  **`prepare-release`** skill at `.claude/skills/prepare-release/`. Run it via
  Claude Code (`/prepare-release`) when cutting a release; it only prepares the
  PR — publishing stays the manual `deploy-nuget.yml` trigger.

## What NOT to do

- Do not add `using` directives already covered by global/implicit usings
- Do not add `// TODO` comments — raise an issue instead
- Do not modify the `*.Sample.*` projects for library behaviour changes (they
  are excluded from code coverage)
- Do not add new public API surface without a corresponding test in
  `TelemetryOptionsTests.cs` or `TelemetryServiceCollectionExtensionsTests.cs`

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
