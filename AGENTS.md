# AGENTS.md

Canonical instructions for AI coding agents working in this repository.
Tool-specific files (`CLAUDE.md`, `.github/copilot-instructions.md`) point here.

## Project

NuGet package that wires up OpenTelemetry (tracing, metrics, logging) for
ASP.NET Core via a single `AddTelemetry()` call and `appsettings.json`.

## Repository layout

```
src/
  OpenTelemetryExtension.Configuration/          # Library (netstandard2.0 + net10.0)
  OpenTelemetryExtension.Configuration.Tests/    # xUnit tests (net10.0)
  OpenTelemetryExtension.Configuration.Sample/   # ASP.NET Core sample app (net10.0)
  OpenTelemetryExtension.slnx                    # Solution file
.github/workflows/
  ci.yml                                         # Build + test + coverage on push
  deploy-nuget.yml                               # Manual NuGet publish + GitHub Release
release-notes/                                   # v{VERSION}.md per release
```

## Public API (2 classes, minimal surface)

```csharp
// IServiceCollection extensions
services.AddTelemetry(configuration);           // binds "Telemetry" section
services.AddTelemetry(o => { o.Enabled = true; o.Endpoint = new Uri("..."); });
```

`TelemetryOptions` is the single configuration model. `Enabled = false` is the
safe default; `AddTelemetry()` is a no-op when disabled. `Endpoint` is
`[Required]` and validated at registration time when `Enabled = true`.

## Build & test

```bash
dotnet build src/OpenTelemetryExtension.slnx -c Release
dotnet test  src/OpenTelemetryExtension.slnx -c Release --filter "Category!=Long-Running"
```

Tests use **xUnit + Moq**. No integration test infrastructure needed — all
tests run in-process via `ServiceCollection`.

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
- Do not use `Thread.Sleep` or `Task.Delay` in tests

## Versioning & release

- Version lives in
  `src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`
  (`<Version>`)
- Do not change `<Version>` without also creating `release-notes/v{VERSION}.md`
- NuGet publish is **manual** (`workflow_dispatch`) — never triggered
  automatically

## What NOT to do

- Do not add `using` directives already covered by global/implicit usings
- Do not add `// TODO` comments — raise an issue instead
- Do not modify the `*.Sample` project for library behaviour changes (it is
  excluded from code coverage)
- Do not add new public API surface without a corresponding test in
  `TelemetryOptionsTests.cs` or `TelemetryServiceCollectionExtensionsTests.cs`

## Adding a new instrumentation option

1. Add `bool EnableXxxInstrumentation { get; set; } = true;` to `TelemetryOptions`
   with XML doc + Default comment
2. Wire it up in `TelemetryServiceCollectionExtensions` under the appropriate
   signal block
3. Add default-value test in `TelemetryOptionsTests.cs`
4. Add enabled/disabled integration tests in
   `TelemetryServiceCollectionExtensionsTests.cs`
5. Add the option to the `<example>` block in the XML doc on `TelemetryOptions`
6. Update `README.md` configuration reference table
