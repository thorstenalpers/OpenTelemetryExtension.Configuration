[![OpenTelemetryExtension.Configuration](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/banner.png)](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration)

[![CI](https://img.shields.io/github/actions/workflow/status/thorstenalpers/OpenTelemetryExtension.Configuration/ci.yml?branch=main&style=flat-square&logo=githubactions&logoColor=white&label=CI)](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/actions/workflows/ci.yml)
[![Coverage](https://img.shields.io/coverallsCoverage/github/thorstenalpers/OpenTelemetryExtension.Configuration?branch=main&style=flat-square&logo=coveralls&label=coverage)](https://coveralls.io/github/thorstenalpers/OpenTelemetryExtension.Configuration?branch=main)
[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Configuration?style=flat-square&logo=nuget&logoColor=white&label=nuget)](https://www.nuget.org/packages/OpenTelemetryExtension.Configuration)
[![Downloads](https://img.shields.io/nuget/dt/OpenTelemetryExtension.Configuration?style=flat-square&logo=nuget&logoColor=white&label=downloads)](https://www.nuget.org/packages/OpenTelemetryExtension.Configuration)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](./LICENSE)

Drop-in OpenTelemetry setup for .NET — **tracing, metrics and logging** over OTLP, configured through code or configuration.

---

## ✨ Features

- **One-call setup** — tracing, metrics and logging via a single `AddTelemetry()`, from `appsettings.json` or code
- **Works on any .NET** — ASP.NET Core, WPF, console and more, exporting over OTLP to any compatible backend
- **Auto-instrumentation** — `HttpClient`, ASP.NET Core and .NET runtime traced and measured automatically, each toggleable
- **Extensible** — add your own sources, meters and databases when you need them

---


## ✅ Requirements

- A .NET target compatible with **`netstandard2.0`** — i.e. .NET Framework 4.6.1+, .NET 6/8/9/10, or directly the **`net8.0`** / **`net10.0`** builds.
- An **OTLP-compatible backend** to receive the telemetry (collector, Jaeger, OpenObserve, the .NET Aspire Dashboard, …). See [Running Locally with a Backend](#-running-locally-with-a-backend).
- ASP.NET Core instrumentation requires a modern .NET target (**`net8.0`** or **`net10.0`** build); it is not included in the `netstandard2.0` build used by WPF/console apps.

---

## 📦 Installation

```bash
dotnet add package OpenTelemetryExtension.Configuration
```

### Other ways to consume it

The NuGet package is the recommended path, but it isn't the only one:

- **As source / project reference or a plain copy** — the library is just two
  files. Either clone
  [`OpenTelemetryExtension.Configuration`](./src/OpenTelemetryExtension.Configuration)
  into your repository and add a `<ProjectReference>` to it, or drop the two files
  straight into your project (no package, no reference) and own them outright:
  [`TelemetryServiceCollectionExtensions.cs`](./src/OpenTelemetryExtension.Configuration/TelemetryServiceCollectionExtensions.cs)
  and
  [`TelemetryOptions.cs`](./src/OpenTelemetryExtension.Configuration/TelemetryOptions.cs).
  Handy when you want to tweak the defaults or step through the setup code.
- **As a git submodule** — pin the source at a specific commit and reference the
  project from your solution:
  ```bash
  git submodule add https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration.git external/OpenTelemetryExtension.Configuration
  ```
  then add a `<ProjectReference>` to
  `external/OpenTelemetryExtension.Configuration/src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`.
- **As a fork** — fork the repository and adapt it to your needs. Optionally, if
  your changes are generally useful, open a pull request back upstream. See
  [Contributing](#-contributing).

In every case the API is identical — `AddTelemetry(...)` works the same whether
the type comes from a NuGet package or your own copy of the source.

---

## 🚀 Quick Start

### 1. Register

```csharp
builder.Services.AddTelemetry(builder.Configuration);
```

### 2. Configure (`appsettings.json`)

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:4318",
    "ServiceName": "my-api"
  }
}
```

That's it — tracing, metrics and logging are exported via OTLP.

> You need an **OTLP-compatible backend** listening at `Endpoint`. No backend yet?
> See [Running Locally with a Backend](#-running-locally-with-a-backend) for one-command setups.

---

## ⚙️ Configuration

All keys live under the **`Telemetry`** section. Only `Endpoint` is required; everything
else has a default and telemetry is on out of the box. Bind from `appsettings.json` or set
the same properties in the `AddTelemetry(o => …)` callback.

| Key | Description | Type | Default | Example |
|---|---|---|---|---|
| `Endpoint` | [OTLP](https://opentelemetry.io/docs/languages/net/exporters/) endpoint for traces, metrics and logs. | `Uri` | — *(required)* | `https://otel.example.com:4317` |
| `Protocol` | [Transport protocol](https://opentelemetry.io/docs/languages/net/exporters/): `HttpProtobuf` (4318) or `Grpc` (4317). | `string` | `HttpProtobuf` | `Grpc` |
| `Headers` | [OTLP exporter headers](https://opentelemetry.io/docs/languages/net/exporters/), e.g. for auth. Format: `k1=v1,k2=v2`. | `string` | `""` | `x-otlp-api-key=secret` |
| `Enabled` | Master switch; `false` registers no OpenTelemetry at all. | `bool` | `true` | `false` |
| `ServiceName` | Logical [service name](https://opentelemetry.io/docs/specs/semconv/resource/) shown in the backend. | `string?` | `null` | `orders-api` |
| `ResourceAttributes` | Extra [resource attributes](https://opentelemetry.io/docs/specs/semconv/resource/). | `object` | `{}` | `{ "deployment.environment": "production" }` |
| `EnableTracing` | Distributed [tracing](https://opentelemetry.io/docs/concepts/signals/traces/). | `bool` | `true` | `false` |
| `EnableMetrics` | [Metrics](https://opentelemetry.io/docs/concepts/signals/metrics/) collection. | `bool` | `true` | `false` |
| `EnableLogging` | [Log](https://opentelemetry.io/docs/concepts/signals/logs/) export via OTLP. | `bool` | `true` | `false` |
| `SampleRatio` | Trace [sample](https://opentelemetry.io/docs/concepts/sampling/) fraction — `1.0` = all, `0.1` = 10% (`ParentBased(TraceIdRatioBased)`). | `double` | `1.0` | `0.1` |
| `EnableAspNetCoreInstrumentation` | Incoming [ASP.NET Core](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore) requests *(net10.0 only; no-op on netstandard2.0)*. | `bool` | `true` | `false` |
| `EnableHttpClientInstrumentation` | Outgoing [`HttpClient`](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Http) requests. | `bool` | `true` | `false` |
| `EnableRuntimeInstrumentation` | [.NET runtime metrics](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.Runtime) (GC, memory, thread pool). | `bool` | `true` | `false` |
| `AdditionalTracingSources` | Extra [`ActivitySource`](https://learn.microsoft.com/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs) names to collect. | `string[]` | `[]` | `[ "Npgsql", "MyApp" ]` |
| `AdditionalMeters` | Extra [`Meter`](https://learn.microsoft.com/dotnet/core/diagnostics/metrics-instrumentation) names to collect. | `string[]` | `[]` | `[ "MyApp.Orders" ]` |
| `RecordExceptions` | Record [exceptions](https://opentelemetry.io/docs/specs/semconv/exceptions/) with stack traces on spans. | `bool` | `true` | `false` |
| `ExcludedPaths` | Paths excluded from tracing (per-segment prefix match). | `string[]` | `[ "/health" ]` | `[ "/health", "/ready" ]` |
| `IncludeScopes` | Include [log scopes](https://learn.microsoft.com/dotnet/core/extensions/logging#log-scopes) in exported records. | `bool` | `true` | `false` |
| `IncludeFormattedMessage` | Include the formatted message in exported records. | `bool` | `true` | `false` |
| `ConfigureTracing` | **Code only.** Register extra [tracing](https://opentelemetry.io/docs/concepts/signals/traces/) — see [Code configuration](#-code-configuration). | `Action<TracerProviderBuilder>` | `null` | `t => t.AddSource("MyApp")` |
| `ConfigureMetrics` | **Code only.** Register extra [metrics](https://opentelemetry.io/docs/concepts/signals/metrics/). | `Action<MeterProviderBuilder>` | `null` | `m => m.AddMeter("MyApp")` |
| `ConfigureLogging` | **Code only.** Tweak the [logging](https://learn.microsoft.com/dotnet/core/extensions/logging) pipeline. | `Action<ILoggingBuilder>` | `null` | `l => l.AddConsole()` |

> 💡 See [`docs/appsettings.Example.json`](./docs/appsettings.Example.json)
> for a complete profile with every key set to a realistic, non-default value.

### Custom section name

The section defaults to `Telemetry`, but you can bind any section by passing its name:

```csharp
builder.Services.AddTelemetry(builder.Configuration, "MyTelemetry");
// or together with a code callback:
builder.Services.AddTelemetry(builder.Configuration, o => { /* ... */ }, "MyTelemetry");
```

```json
{
  "MyTelemetry": {
    "Endpoint": "http://localhost:4318",
    "ServiceName": "my-api"
  }
}
```

---

## 🧩 Code Configuration

Configure entirely in code instead of `appsettings.json`:

```csharp
builder.Services.AddTelemetry(o =>
{
    o.Endpoint        = new Uri("http://localhost:4318");
    o.ServiceName     = "my-api";
    o.ResourceAttributes = new() { ["deployment.environment"] = "production" };
    o.SampleRatio     = 0.1;

    // Code-only: register additional instrumentation
    o.ConfigureTracing = tracing => tracing.AddSource("MyApp");
    o.ConfigureMetrics = metrics => metrics.AddMeter("MyApp");
    o.ConfigureLogging = logging => logging.AddConsole();
});
```

Or bind `appsettings.json` first and layer code-only options on top — both
sources are combined, and bound values can still be overridden in the callback:

```csharp
builder.Services.AddTelemetry(builder.Configuration, o =>
{
    // Everything from appsettings.json is already bound here.
    o.ConfigureTracing = tracing => tracing.AddSource("MyApp");
    o.ConfigureMetrics = metrics => metrics.AddMeter("MyApp");
    o.ConfigureLogging = logging => logging.AddConsole();
});
```

### The `Configure*` hooks — Sources & Meters

The three callbacks are the extension points for **your own** telemetry. The
built-in instrumentation (ASP.NET Core, `HttpClient`, SQL, runtime) is wired up
automatically; these hooks let you add the signals your application emits itself.

| Hook | Builder | Used to register |
|---|---|---|
| `ConfigureTracing` | [`TracerProviderBuilder`](https://opentelemetry.io/docs/languages/net/instrumentation/#traces) | **Activity Sources** via `AddSource("Name")` |
| `ConfigureMetrics` | [`MeterProviderBuilder`](https://opentelemetry.io/docs/languages/net/instrumentation/#metrics) | **Meters** via `AddMeter("Name")` |
| `ConfigureLogging` | [`ILoggingBuilder`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.iloggingbuilder) | extra logging providers, filters, etc. |

**What is a Meter?**
A [`Meter`](https://learn.microsoft.com/dotnet/api/system.diagnostics.metrics.meter)
(from `System.Diagnostics.Metrics`) is the factory you create instruments
(counters, histograms, gauges) from. Each `Meter` has a **name**, and OpenTelemetry
only collects metrics from meters you have explicitly registered with
`AddMeter("That.Name")`. Without that call, your custom metrics are never exported.

```csharp
// 1. Create a Meter and an instrument somewhere in your app
private static readonly Meter Meter = new("MyApp.Orders");
private static readonly Counter<long> OrdersPlaced = Meter.CreateCounter<long>("orders.placed");

// ... later
OrdersPlaced.Add(1);

// 2. Register the meter's name so it gets exported
o.ConfigureMetrics = metrics => metrics.AddMeter("MyApp.Orders");
```

**What is a Source?**
An [`ActivitySource`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource)
is the tracing equivalent: it creates `Activity` objects (= spans). Register its
name with `AddSource("MyApp")` so your custom spans are sampled and exported.

```csharp
private static readonly ActivitySource Activity = new("MyApp");

using var span = Activity.StartActivity("ProcessOrder");
// ... work being traced

o.ConfigureTracing = tracing => tracing.AddSource("MyApp");
```

> The string passed to `AddMeter`/`AddSource` must **exactly match** the name you
> gave the `Meter`/`ActivitySource` — that name is how OpenTelemetry routes the
> data.

### Databases

Database instrumentation is **not** built in — it depends entirely on your
driver, so it is added through the `ConfigureTracing` hook. This keeps the
package free of database-specific dependencies; you only pull in what you use.

```csharp
// SQL Server — install the package, then register it:
//   dotnet add package OpenTelemetry.Instrumentation.SqlClient
o.ConfigureTracing = t => t.AddSqlClientInstrumentation();

// EF Core — dedicated instrumentation package:
//   dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
o.ConfigureTracing = t => t.AddEntityFrameworkCoreInstrumentation();

// Drivers with a built-in ActivitySource — just register its name:
o.ConfigureTracing = t => t.AddSource("Npgsql");          // PostgreSQL (Npgsql)
o.ConfigureTracing = t => t.AddSource("MySqlConnector");  // MySQL (MySqlConnector)
```

Oracle (`Oracle.ManagedDataAccess.Core`) emits an `ActivitySource` in recent
versions and is wired up the same way via `AddSource(...)`.

**No code for source-based drivers:** if the driver only needs an `ActivitySource`
name (Npgsql, MySqlConnector, Oracle, your own app sources), you can enable it
purely from `appsettings.json` — no `ConfigureTracing` call required:

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:4318",
    "AdditionalTracingSources": [ "Npgsql", "MyApp" ],
    "AdditionalMeters": [ "MyApp.Orders" ]
  }
}
```

> Package-based instrumentation (SQL Server, EF Core) still needs the one-line
> `ConfigureTracing` call above, because it requires its NuGet package — a config
> string alone can't pull in a dependency.

**Toggling SQL instrumentation from `appsettings.json`**

Because `EnableSqlClientInstrumentation` is not part of `TelemetryOptions` (the
package is optional), you can add it as a custom key and read it in the callback:

`appsettings.json`:

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:4318",
    "ServiceName": "my-api",
    "RecordExceptions": true,

    "EnableSqlClientInstrumentation": true   // custom key
  }
}
```

`Program.cs`:

```csharp
builder.Services.AddTelemetry(builder.Configuration, opt =>
    opt.ConfigureTracing = tracing =>
    {
        if (builder.Configuration.GetValue<bool>("Telemetry:EnableSqlClientInstrumentation"))
        {
            // Microsoft SQL Server / System.Data.SqlClient
            // NuGet: OpenTelemetry.Instrumentation.SqlClient
            tracing.AddSqlClientInstrumentation(sql => sql.RecordException = opt.RecordExceptions);

            // PostgreSQL (Npgsql)
            // NuGet: OpenTelemetry.Instrumentation.Npgsql
            tracing.AddNpgsql();

            // MySQL (MySqlConnector)
            // NuGet: OpenTelemetry.Instrumentation.MySqlData
            tracing.AddMySqlDataInstrumentation();
        }
    });
```

This keeps the on/off switch in config while the package dependency stays explicit in code.

---

## 🖥️ Using outside the Generic Host

`AddTelemetry()` works with **any** `IServiceCollection` — ASP.NET Core, WPF,
WinForms, console, MAUI/WinUI, UWP, worker services, etc.

**With the Generic Host** (recommended for desktop/console — `Host.CreateApplicationBuilder()`),
the providers start and flush automatically:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTelemetry(builder.Configuration);
using var host = builder.Build();
await host.RunAsync();   // telemetry starts here and flushes on shutdown
```

**Without a host** (e.g. a bare `ServiceCollection` in UWP or a minimal app),
build the provider and **dispose it on exit** so buffered telemetry is flushed:

```csharp
var services = new ServiceCollection();
services.AddTelemetry(o =>
{
    o.Endpoint    = new Uri("http://localhost:4318");
    o.ServiceName = "my-desktop-app";
});

var provider = services.BuildServiceProvider();
// ... app runs ...
provider.Dispose();      // flushes traces, metrics and logs
```

> ASP.NET Core instrumentation is only in the `net10.0` build. On the
> `netstandard2.0` build (WPF/WinForms/console/UWP) it is simply absent —
> setting `EnableAspNetCoreInstrumentation` there is a harmless no-op.

---

## 🧪 Samples

Two runnable samples live under [`src/`](./src):

| Sample | Project | Demonstrates |
|---|---|---|
| **Web API** | [`…Sample.WebApi`](./src/OpenTelemetryExtension.Configuration.Sample.WebApi) | ASP.NET Core minimal API configured from `appsettings.json`, ready-to-run backend profiles, EF Core and opt-in SQL instrumentation. |
| **WPF** | [`…Sample.Wpf`](./src/OpenTelemetryExtension.Configuration.Sample.Wpf) | Desktop app wiring `AddTelemetry()` through the **Generic Host**, emitting a custom `ActivitySource`/`Meter` and an `HttpClient` span on a button click. |

The Web API sample drives the backend walkthrough below; the WPF sample exports
to `http://localhost:4318` by default — point it at any of the backends here.

---

## 🔌 Running Locally with a Backend

The [Web API sample](./src/OpenTelemetryExtension.Configuration.Sample.WebApi) ships
ready-to-run configurations for several popular backends (the three below are
documented in full; more start scripts live in [`infrastructure/`](./infrastructure)). Each backend has:

1. an **infrastructure start script** (Docker Compose or Helm) in [`infrastructure/`](./infrastructure),
2. a **launch profile** that selects the matching `appsettings.<env>.json`,
3. a **UI** where the exported traces, metrics and logs show up.

### Steps

1. **Start the backend infrastructure** — run the script for your backend (see table).
   - Docker scripts live in [`infrastructure/docker`](./infrastructure/docker) and need Docker.
   - Helm scripts live in [`infrastructure/helm`](./infrastructure/helm) and need a local Kubernetes cluster (e.g. k3s in WSL2).
2. **Run the sample** with the matching profile:
   ```bash
   cd src/OpenTelemetryExtension.Configuration.Sample.WebApi
   dotnet run --launch-profile "Start Aspire"
   ```
   Or pick the profile from the run dropdown in Visual Studio / Rider.
3. **Generate traffic** — the app opens Swagger at `https://localhost:5073/swagger`; call an endpoint.
4. **Open the backend UI** (see table) to inspect the telemetry.

### Backend overview

| Backend | Start infrastructure | Launch profile | Backend UI |
|---|---|---|---|
| .NET Aspire Dashboard | `infrastructure/docker/docker-install-aspire-dashboard.cmd` *(or Helm: `helm/helm-install-aspire-dashboard.cmd`)* | `Start Aspire` | <http://localhost:31888> |
| Jaeger | `infrastructure/docker/docker-install-jaeger.cmd` | `Start Jaeger` | <http://localhost:16686> |
| OpenObserve | `infrastructure/helm/helm-install-openobserve.cmd` | `Start OpenObserve Http` / `Start OpenObserve Grpc` | <http://localhost:30117> (`admin@web.de`/`admin`) |

> **Tip — viewing logs in the Aspire Dashboard:** after starting the app with the
> `Start Aspire` profile, open <http://localhost:31888>, then go to the
> **Structured** (logs), **Traces** or **Metrics** tab. Data appears as soon as
> you hit a Swagger endpoint.

---

## 📝 Sample Backend Configurations

These are the exact `appsettings.<env>.json` files used by the sample's launch profiles.

### .NET Aspire Dashboard — `appsettings.aspire.json`

The dashboard requires an API key on the OTLP endpoint (`x-otlp-api-key`). The
gRPC endpoint is exposed on NodePort `31889` (Helm) or host port `31889` (Docker).

```json
{
  "Telemetry": {
    "Protocol": "Grpc",
    "Endpoint": "http://localhost:31889",
    "Headers": "x-otlp-api-key=aspire"
  }
}
```

Traces, metrics and logs from the sample app shown live in the Aspire Dashboard UI:

![Aspire Dashboard demo](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/Aspire-Dashboard.webp)

### Jaeger — `appsettings.jaeger.json`

```json
{
  "Telemetry": {
    "Protocol": "Grpc",
    "Endpoint": "http://localhost:4317"
  }
}
```

Traces from the sample app shown in the Jaeger UI:

![Jaeger demo](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/Jaeger.webp)

### OpenObserve — HTTP/protobuf — `appsettings.openobserve-http.json`

```json
{
  "Telemetry": {
    "Protocol": "HttpProtobuf",
    "Endpoint": "http://localhost:30117/api/default",
    "Headers": "Authorization=Basic YWRtaW5Ad2ViLmRlOmFkbWlu,stream-name=default"
  }
}
```

The same telemetry explored in the OpenObserve UI:

![OpenObserve demo](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/OpenObserve.webp)

---

## 🤝 Contributing

Contributions are welcome — bug fixes, new instrumentation options, documentation
improvements and ideas alike. For anything beyond a small fix, please open an
issue first so we can agree on the approach before you invest time in a pull
request.

The basic workflow is: fork the repo, branch off `main` (`feature/<name>` or
`fix/<name>`), make your change, add or update tests for any public API change,
run the unit tests, and open a PR against `main`. Build and test locally with:

```bash
dotnet build OpenTelemetryExtension.slnx -c Release
dotnet test  src/OpenTelemetryExtension.Configuration.Tests -c Release
```

Full details — code style, the integration-test stack, and the release process —
are in [CONTRIBUTING.md](./CONTRIBUTING.md). Note that versioning and release
notes are handled separately at release time, so you don't need to touch them in
a feature PR.

## 🐛 Report a Bug

Found something broken or behaving unexpectedly? Please
[open an issue](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/issues/new/choose).
To help reproduce it quickly, include the package version, your target framework
(e.g. `net10.0` or `netstandard2.0`), the relevant `Telemetry` configuration, the
OTLP backend you export to, and what you expected to happen versus what actually
did. A minimal repro or stack trace speeds things up a lot.

For security-sensitive reports, please follow [SECURITY.md](./SECURITY.md)
instead of opening a public issue.
