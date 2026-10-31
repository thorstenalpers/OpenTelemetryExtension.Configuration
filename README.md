[![OpenTelemetryExtension.Configuration](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/banner.png)](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration)

[![CI](https://img.shields.io/github/actions/workflow/status/thorstenalpers/OpenTelemetryExtension.Configuration/ci.yml?branch=main&style=flat-square&logo=githubactions&logoColor=white&label=CI)](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/actions/workflows/ci.yml)
[![Coverage](https://img.shields.io/coverallsCoverage/github/thorstenalpers/OpenTelemetryExtension.Configuration?branch=main&style=flat-square&logo=coveralls&label=coverage)](https://coveralls.io/github/thorstenalpers/OpenTelemetryExtension.Configuration?branch=main)
[![NuGet](https://img.shields.io/nuget/v/OpenTelemetryExtension.Configuration?style=flat-square&logo=nuget&logoColor=white&label=nuget)](https://www.nuget.org/packages/OpenTelemetryExtension.Configuration)
[![Downloads](https://img.shields.io/nuget/dt/OpenTelemetryExtension.Configuration?style=flat-square&logo=nuget&logoColor=white&label=downloads)](https://www.nuget.org/packages/OpenTelemetryExtension.Configuration)
[![License](https://img.shields.io/badge/license-MIT-blue?style=flat-square)](./LICENSE)

**Production-ready OpenTelemetry for .NET in one line.** Traces, metrics and logs
are exported over OTLP to Jaeger, Grafana, SigNoz, OpenObserve, the .NET Aspire
Dashboard or any other OpenTelemetry backend — configured from
`appsettings.json`, with sensible defaults and no boilerplate.

**[Quick Start](#-quick-start) · [Configuration](#-configuration) · [Backends](#-backends) · [Samples](#-samples) · [FAQ](#-faq)**

---

## 🚀 Quick Start

```bash
dotnet add package OpenTelemetryExtension.Configuration
```

```csharp
builder.Services.AddTelemetry(builder.Configuration);
```

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:4318",
    "ServiceName": "my-api"
  }
}
```

That's the whole setup. Incoming requests, outgoing `HttpClient` calls, .NET
runtime metrics and every `ILogger` message now show up in your backend.

No backend yet? The .NET Aspire Dashboard is a single container — run it, start
your app and open <http://localhost:18888>:

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 -p 4318:18890 -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true mcr.microsoft.com/aspire/dashboard:latest
```

![Traces, metrics and logs from the sample app in the .NET Aspire Dashboard](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/Aspire-Dashboard.webp)

<details>
<summary><b>Other ways to consume it</b> — source copy, git submodule, fork</summary>

<br>

- **Source copy or project reference** — the library is just two files:
  [`TelemetryServiceCollectionExtensions.cs`](./src/OpenTelemetryExtension.Configuration/TelemetryServiceCollectionExtensions.cs)
  and [`TelemetryOptions.cs`](./src/OpenTelemetryExtension.Configuration/TelemetryOptions.cs).
  Copy them into your project, or reference
  [the project](./src/OpenTelemetryExtension.Configuration) via `<ProjectReference>`.
- **Git submodule** — pin the source at a specific commit, then add a
  `<ProjectReference>` to
  `external/OpenTelemetryExtension.Configuration/src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`:
  ```bash
  git submodule add https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration.git external/OpenTelemetryExtension.Configuration
  ```
- **Fork** — adapt it, and open a pull request upstream if your change is generally useful.

The API is identical in every case.

</details>

---

## ✨ Features

- 🚀 **One call, three signals** — tracing, metrics and logging via `AddTelemetry()`.
- ⚙️ **Configuration-driven** — every option bindable from `appsettings.json`,
  environment variables or any `IConfiguration` source; override in code when needed.
- 🔋 **Batteries included** — ASP.NET Core, `HttpClient` and .NET runtime
  instrumentation, each toggleable. Parent-based sampling, exceptions on spans
  and `/health` excluded by default.
- 🌍 **Runs everywhere** — ASP.NET Core, worker, console, WPF, WinForms, MAUI and
  .NET Framework via `netstandard2.0`.
- 🪶 **No lock-in** — plain OpenTelemetry underneath, two source files, any OTLP
  backend over HTTP/protobuf or gRPC.

<details>
<summary><b>What <code>AddTelemetry()</code> replaces</b></summary>

<br>

Wiring OpenTelemetry up by hand means five NuGet packages to keep in sync and a
block of setup code that gets copied from service to service — and drifts:

```csharp
var endpoint = new Uri(builder.Configuration["Otlp:Endpoint"]!);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("my-api"))
    .WithTracing(t => t
        .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(1.0)))
        .AddAspNetCoreInstrumentation(o =>
        {
            o.RecordException = true;
            o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
        })
        .AddHttpClientInstrumentation(o => o.RecordException = true)
        .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint, "v1/traces")))
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri(endpoint, "v1/metrics")));

builder.Logging.AddOpenTelemetry(l =>
{
    l.IncludeScopes = true;
    l.IncludeFormattedMessage = true;
    l.AddOtlpExporter(o => o.Endpoint = new Uri(endpoint, "v1/logs"));
});
```

With this package all of it is driven by configuration: switching from Jaeger to
SigNoz, changing the sample rate, adding an auth header or turning telemetry off
in one environment is a config change, not a code change.

</details>

---

## ✅ Requirements

Any app type on **.NET 8+** gets the full build. **.NET Framework 4.6.1+** gets
the `netstandard2.0` build, which has everything except the ASP.NET Core
instrumentation. You also need an OTLP-compatible [backend](#-backends).

<details>
<summary><b>Which build does my app get?</b></summary>

<br>

NuGet picks the build from your **target framework**; the app type plays no role:

| Your target framework | Build you get | ASP.NET Core instrumentation |
|---|---|---|
| `net10.0` — including `net10.0-windows` (WPF/WinForms) | `net10.0` | ✅ |
| `net8.0`, `net9.0` — including their `-windows` variants | `net8.0` | ✅ |
| .NET Framework 4.6.1+, .NET Standard 2.0 consumers | `netstandard2.0` | ❌ |

ASP.NET Core instrumentation traces *incoming HTTP requests*, so it is left out of
the `netstandard2.0` build to keep .NET Framework clients free of an ASP.NET Core
dependency. OTLP export, tracing, metrics, logging, `HttpClient` and runtime
instrumentation are identical in all three builds.

</details>

---

## 🔧 Configuration

All keys live under the **`Telemetry`** section. Only `Endpoint` is required;
everything else has a default. The same properties can be set in the
`AddTelemetry(o => …)` callback.

| Key | Type | Default | Description |
|---|---|---|---|
| **Connection** | | | |
| `Endpoint` | `Uri` | *required* | OTLP base URL for all signals. |
| `Protocol` | `string` | `HttpProtobuf` | `HttpProtobuf` (port 4318) or `Grpc` (port 4317). |
| `Headers` | `string` | `""` | Exporter headers, e.g. for auth: `k1=v1,k2=v2`. |
| **Identity** | | | |
| `ServiceName` | `string?` | `null` | Service name shown in the backend. |
| `ResourceAttributes` | `object` | `{}` | Extra resource attributes, e.g. `deployment.environment`. |
| **Signals** | | | |
| `Enabled` | `bool` | `true` | Master switch; `false` registers nothing and skips validation. |
| `EnableTracing` | `bool` | `true` | Distributed tracing. |
| `EnableMetrics` | `bool` | `true` | Metrics collection. |
| `EnableLogging` | `bool` | `true` | `ILogger` export via OTLP. |
| **Instrumentation** | | | |
| `EnableAspNetCoreInstrumentation` | `bool` | `true` | Incoming ASP.NET Core requests *(no-op on `netstandard2.0`)*. |
| `EnableHttpClientInstrumentation` | `bool` | `true` | Outgoing `HttpClient` requests. |
| `EnableRuntimeInstrumentation` | `bool` | `true` | GC, memory and thread-pool metrics. |
| `RecordExceptions` | `bool` | `true` | Record exceptions with stack traces on spans. |
| `ExcludedPaths` | `string[]` | `["/health"]` | Paths excluded from tracing (per-segment prefix match). |
| `AdditionalTracingSources` | `string[]` | `[]` | Extra `ActivitySource` names to collect. |
| `AdditionalMeters` | `string[]` | `[]` | Extra `Meter` names to collect. |
| `SampleRatio` | `double` | `1.0` | Fraction of traces sampled, parent-based. |
| **Logging** | | | |
| `IncludeScopes` | `bool` | `true` | Include log scopes in exported records. |
| `IncludeFormattedMessage` | `bool` | `true` | Include the formatted message in exported records. |
| **Code only** | | | |
| `ConfigureTracing` | `Action<TracerProviderBuilder>` | `null` | Register extra sources or instrumentation. |
| `ConfigureMetrics` | `Action<MeterProviderBuilder>` | `null` | Register extra meters. |
| `ConfigureLogging` | `Action<ILoggingBuilder>` | `null` | Tweak the logging pipeline. |

> 💡 [`docs/appsettings.Example.json`](./docs/appsettings.Example.json) sets every
> key to a realistic, non-default value.

**Endpoint & protocol.** Give `Endpoint` the *base* URL. With `HttpProtobuf` the
per-signal path (`/v1/traces`, `/v1/metrics`, `/v1/logs`) is appended for you;
with `Grpc` every signal goes to the base URL as-is. `AddTelemetry()` throws at
registration when `Endpoint` is missing — unless `Enabled` is `false`.

<details>
<summary><b>Configure in code</b> — callback only, or on top of <code>appsettings.json</code></summary>

<br>

```csharp
builder.Services.AddTelemetry(o =>
{
    o.Endpoint           = new Uri("http://localhost:4318");
    o.ServiceName        = "my-api";
    o.ResourceAttributes = new() { ["deployment.environment"] = "production" };
    o.SampleRatio        = 0.1;
    o.ConfigureTracing   = tracing => tracing.AddSource("MyApp");
});
```

Bind `appsettings.json` first and layer code on top — bound values can still be
overridden in the callback:

```csharp
builder.Services.AddTelemetry(builder.Configuration, o =>
    o.ConfigureTracing = tracing => tracing.AddSource("MyApp"));
```

</details>

<details>
<summary><b>Custom section name</b></summary>

<br>

```csharp
builder.Services.AddTelemetry(builder.Configuration, "MyTelemetry");
builder.Services.AddTelemetry(builder.Configuration, o => { /* ... */ }, "MyTelemetry");
```

</details>

<details>
<summary><b>Your own spans and metrics</b> — <code>ActivitySource</code> and <code>Meter</code></summary>

<br>

OpenTelemetry only collects sources and meters whose name you registered — via
`AdditionalTracingSources` / `AdditionalMeters` in config, or in code:

```csharp
private static readonly ActivitySource Activity = new("MyApp");
private static readonly Meter Meter = new("MyApp.Orders");
private static readonly Counter<long> OrdersPlaced = Meter.CreateCounter<long>("orders.placed");

using var span = Activity.StartActivity("ProcessOrder");
OrdersPlaced.Add(1);

o.ConfigureTracing = tracing => tracing.AddSource("MyApp");
o.ConfigureMetrics = metrics => metrics.AddMeter("MyApp.Orders");
```

The registered name must **exactly match** the name of the `ActivitySource` or `Meter`.

</details>

<details>
<summary><b>Databases</b> — SQL Server, EF Core, PostgreSQL, MySQL, Oracle</summary>

<br>

Database instrumentation depends on your driver, so it is not built in:

| Kind | Examples | How to enable |
|---|---|---|
| **Package-based** | SQL Server (`OpenTelemetry.Instrumentation.SqlClient`), EF Core (`OpenTelemetry.Instrumentation.EntityFrameworkCore`) | install the package and call it in `ConfigureTracing` |
| **Source-based** | Npgsql (`Npgsql`), MySqlConnector (`MySqlConnector`), Oracle (`Oracle.ManagedDataAccess.Core`) | register the source name — config or code |

`ConfigureTracing` is a single delegate, so chain everything in one assignment:

```csharp
o.ConfigureTracing = t => t
    .AddSqlClientInstrumentation()
    .AddEntityFrameworkCoreInstrumentation()
    .AddSource("Npgsql")
    .AddSource("MySqlConnector");
```

Source-based drivers need no code at all:

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:4318",
    "AdditionalTracingSources": [ "Npgsql" ]
  }
}
```

To toggle an optional package from config, add your own key and read it in the callback:

```csharp
builder.Services.AddTelemetry(builder.Configuration, opt =>
    opt.ConfigureTracing = tracing =>
    {
        if (builder.Configuration.GetValue<bool>("Telemetry:EnableSqlClientInstrumentation"))
        {
            tracing.AddSqlClientInstrumentation(sql => sql.RecordException = opt.RecordExceptions);
        }
    });
```

</details>

<details>
<summary><b>Console and desktop apps</b> — with or without the Generic Host</summary>

<br>

`AddTelemetry()` works with any `IServiceCollection`. With the Generic Host the
providers start and flush automatically:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTelemetry(builder.Configuration);
using var host = builder.Build();
await host.RunAsync();
```

Without a host, **dispose the provider on exit** so buffered telemetry is flushed:

```csharp
var services = new ServiceCollection();
services.AddTelemetry(o => o.Endpoint = new Uri("http://localhost:4318"));

var provider = services.BuildServiceProvider();
// ... app runs ...
provider.Dispose();
```

</details>

---

## 🔌 Backends

Anything that speaks [OTLP](https://opentelemetry.io/docs/specs/otlp/) works. For a
hosted service (Grafana Cloud, Honeycomb, New Relic, Elastic, Datadog, …) point
`Endpoint` at its OTLP ingest URL and put the API key in `Headers`.

These backends ship with a start script in [`infrastructure/`](./infrastructure)
and a matching launch profile in the [Web API sample](./src/OpenTelemetryExtension.Configuration.Sample.WebApi):

| Backend | Start script | Launch profile | UI |
|---|---|---|---|
| [.NET Aspire Dashboard](https://aspire.dev/dashboard/standalone/) | `docker/docker-install-aspire-dashboard.cmd` | `Start Aspire` | <http://localhost:31888> |
| [Jaeger](https://www.jaegertracing.io/) | `docker/docker-install-jaeger.cmd` | `Start Jaeger` | <http://localhost:16686> |
| [OpenObserve](https://openobserve.ai/) | `helm/helm-install-openobserve.cmd` | `Start OpenObserve Http` / `Grpc` | <http://localhost:30117> |
| [SigNoz](https://signoz.io/) | `helm/helm-install-signoz.cmd` | `Start SigNoz` | <http://localhost:30111> |
| [Grafana](https://grafana.com/) / [Loki](https://grafana.com/oss/loki/) | `docker/docker-install-loki.cmd` | `Start Loki` | <http://localhost:3000> |
| [OpenSearch Dashboards](https://opensearch.org/) | `docker/docker-install-opensearch.cmd` | `Start OpenSearch` | <http://localhost:5601> |

Start the backend, then run the sample with the matching profile and call an
endpoint from Swagger (`https://localhost:5073/swagger`):

```bash
cd src/OpenTelemetryExtension.Configuration.Sample.WebApi
dotnet run --launch-profile "Start Aspire"
```

Docker scripts need Docker; Helm scripts need a local Kubernetes cluster (e.g. k3s
in WSL2). Aspire also has a Helm script and OpenObserve a Docker one. Local logins:
OpenObserve `admin@web.de`/`admin`, SigNoz `admin@web.de`, Grafana `admin`/`admin`.

<details>
<summary><b>.NET Aspire Dashboard</b> — <code>appsettings.aspire.json</code></summary>

<br>

The dashboard requires an API key on the OTLP endpoint. gRPC is exposed on port
`31889`; open <http://localhost:31888> and use the **Structured**, **Traces** or
**Metrics** tab.

```json
{
  "Telemetry": {
    "Protocol": "Grpc",
    "Endpoint": "http://localhost:31889",
    "Headers": "x-otlp-api-key=aspire"
  }
}
```

</details>

<details>
<summary><b>Jaeger</b> — <code>appsettings.jaeger.json</code></summary>

<br>

```json
{
  "Telemetry": {
    "Protocol": "Grpc",
    "Endpoint": "http://localhost:4317"
  }
}
```

![Traces from the sample app in the Jaeger UI](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/Jaeger.webp)

</details>

<details>
<summary><b>OpenObserve</b> — <code>appsettings.openobserve-http.json</code></summary>

<br>

```json
{
  "Telemetry": {
    "Protocol": "HttpProtobuf",
    "Endpoint": "http://localhost:30117/api/default",
    "Headers": "Authorization=Basic YWRtaW5Ad2ViLmRlOmFkbWlu,stream-name=default"
  }
}
```

![Telemetry from the sample app in the OpenObserve UI](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/OpenObserve.webp)

</details>

---

## 🧪 Samples

| Sample | Demonstrates |
|---|---|
| [**Web API**](./src/OpenTelemetryExtension.Configuration.Sample.WebApi) | ASP.NET Core minimal API configured from `appsettings.json`, backend launch profiles, EF Core and opt-in SQL instrumentation. |
| [**WPF**](./src/OpenTelemetryExtension.Configuration.Sample.Wpf) | Desktop app on the Generic Host emitting a custom `ActivitySource`, `Meter` and `HttpClient` span; exports to `http://localhost:4318`. |

---

## ❓ FAQ

<details>
<summary><b>Does it hide OpenTelemetry from me?</b></summary>

No. It calls the official OpenTelemetry .NET SDK and exposes the underlying
builders through `ConfigureTracing`, `ConfigureMetrics` and `ConfigureLogging`.
Anything the SDK can do, you can still do.

</details>

<details>
<summary><b>How do I turn telemetry off in tests or locally?</b></summary>

Set `"Enabled": false` (or `Telemetry__Enabled=false`). Nothing is registered and
no endpoint is required.

</details>

<details>
<summary><b>Can I use it with .NET Aspire?</b></summary>

Yes. Point `Endpoint` at the Aspire Dashboard's OTLP endpoint — also from projects
that are not part of an Aspire AppHost but should report to the same dashboard.

</details>

<details>
<summary><b>Why is my custom metric or span missing?</b></summary>

Its `Meter`/`ActivitySource` name is not registered. Add it to
`AdditionalMeters`/`AdditionalTracingSources` or call `AddMeter`/`AddSource` —
the names must match exactly.

</details>

---

## 🤝 Contributing

Bug fixes, new options and documentation improvements are welcome — for anything
beyond a small fix, please open an issue first. Build, test and style details are
in [CONTRIBUTING.md](./CONTRIBUTING.md).

Found a bug? [Open an issue](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/issues/new/choose)
with the package version, target framework, your `Telemetry` configuration and
the backend. Security issues go through [SECURITY.md](./SECURITY.md).

Released under the [MIT License](./LICENSE). If this package saves you time, a ⭐
helps others find it.
