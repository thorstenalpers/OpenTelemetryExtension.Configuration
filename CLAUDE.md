# OpenTelemetryExtension.Configuration

NuGet package that wires up OpenTelemetry (tracing, metrics, logging) for ASP.NET Core via a single `AddTelemetry()` call and `appsettings.json`.

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

// Configuration model
public sealed class TelemetryOptions
{
    public bool    Enabled                         { get; set; } = false;
    public Uri?    Endpoint                        { get; set; }   // [Required]
    public string  Headers                         { get; set; } = "";
    public OtlpExportProtocol Protocol             { get; set; } = HttpProtobuf;
    public string? ServiceName                     { get; set; }
    public string? EnvironmentName                 { get; set; }
    public Dictionary<string, string> ResourceAttributes { get; set; } = [];
    public double  SampleRatio                     { get; set; } = 1.0;
    public bool    EnableTracing                   { get; set; } = true;
    public bool    EnableMetrics                   { get; set; } = true;
    public bool    EnableLogging                   { get; set; } = true;
    public bool    EnableAspNetCoreInstrumentation { get; set; } = true;
    public bool    EnableHttpClientInstrumentation { get; set; } = true;
    public bool    EnableSqlClientInstrumentation  { get; set; } = false;
    public bool    EnableRuntimeInstrumentation    { get; set; } = true;
    public bool    RecordExceptions                { get; set; } = true;
    public string[] ExcludedPaths                  { get; set; } = ["/health"];
    public bool    IncludeScopes                   { get; set; } = true;
    public bool    IncludeFormattedMessage         { get; set; } = true;
    public Action<TracerProviderBuilder>? ConfigureTracing   { get; set; }
    public Action<MeterProviderBuilder>?  ConfigureMetrics   { get; set; }
    public Action<ILoggingBuilder>?       ConfigureLogging   { get; set; }
}
```

## Build & test

```bash
dotnet build src/OpenTelemetryExtension.slnx -c Release
dotnet test  src/OpenTelemetryExtension.slnx -c Release --filter "Category!=Long-Running"
```

Tests use **xUnit + Moq**. No integration test infrastructure needed — all tests run in-process via `ServiceCollection`.

## Code conventions

- **File-scoped namespaces** required (`namespace Foo;` not `namespace Foo { }`)
- **Nullable reference types** enabled everywhere (`<Nullable>enable</Nullable>`)
- **EditorConfig** is enforced at build time (`EnforceCodeStyleInBuild=True`) — do not bypass it
- Private fields: `_camelCase`, static fields: `s_camelCase`, interfaces: `IFoo`, type params: `TFoo`
- No comments explaining *what* code does — only *why* (hidden constraints, workarounds)
- No trailing XML doc blocks on self-explanatory members

## Versioning & release

- Version lives in `src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj` (`<Version>`)
- Release notes go in `release-notes/v{VERSION}.md` **before** triggering `deploy-nuget.yml`
- NuGet publish is **manual** (`workflow_dispatch`) — never triggered automatically

## Key constraints

- `netstandard2.0` target must be kept — do not use APIs unavailable there without a `#if NET5_0_OR_GREATER` guard
- `Enabled = false` is the safe default; `AddTelemetry()` must be a no-op when disabled
- `Endpoint` is `[Required]` — validation runs via `DataAnnotations` at registration time
- The sample project (`*.Sample`) is excluded from code coverage (`<ExcludeFromCodeCoverage>true</ExcludeFromCodeCoverage>`)
- Do not add new public API surface without a corresponding test in `TelemetryOptionsTests.cs` or `TelemetryServiceCollectionExtensionsTests.cs`

## Adding a new instrumentation option

1. Add `bool EnableXxxInstrumentation { get; set; } = true;` to `TelemetryOptions` with XML doc + Default comment
2. Wire it up in `TelemetryServiceCollectionExtensions` under the appropriate signal block
3. Add default-value test in `TelemetryOptionsTests.cs`
4. Add enabled/disabled integration tests in `TelemetryServiceCollectionExtensionsTests.cs`
5. Add the option to the `<example>` block in the XML doc on `TelemetryOptions`
6. Update `README.md` configuration reference table
