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

![Traces, metrics and logs from the sample app in the .NET Aspire Dashboard](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/Aspire-Dashboard.webp)

---

## Contents

- [🤔 Why this package?](#-why-this-package)
- [✨ Features](#-features)
- [🔭 Works with your backend](#-works-with-your-backend)
- **Getting started**
  - [✅ Requirements](#-requirements)
  - [📦 Installation](#-installation) · [Other ways to consume it](#other-ways-to-consume-it)
  - [🚀 Quick Start](#-quick-start)
- **Configuring telemetry**
  - [⚙️ Configuration](#️-configuration) · [Options reference](#options-reference) · [Endpoint & protocol](#endpoint--protocol) · [Custom section name](#custom-section-name)
  - [🧩 Code Configuration](#-code-configuration) · [Registering your own Sources & Meters](#registering-your-own-sources--meters) · [Databases](#databases)
  - [🖥️ Using outside the Generic Host](#️-using-outside-the-generic-host)
- **Trying it out**
  - [🧪 Samples](#-samples)
  - [🔌 Running Locally with a Backend](#-running-locally-with-a-backend) · [Sample backend configurations](#sample-backend-configurations)
- **Project**
  - [❓ FAQ](#-faq)
  - [🤝 Contributing](#-contributing)
  - [🐛 Report a Bug](#-report-a-bug)
  - [📄 License](#-license)

---

## 🤔 Why this package?

OpenTelemetry for .NET is powerful, but wiring it up by hand means five NuGet
packages to keep in sync and a block of setup code that gets copied from
service to service — and drifts. This is roughly what `AddTelemetry()` replaces:

```csharp
// Without this package
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

```csharp
// With this package
builder.Services.AddTelemetry(builder.Configuration);
```

Everything in the first block is still there — it is just driven by
configuration. Switching from Jaeger to SigNoz, changing the sample rate,
adding an auth header or turning telemetry off in one environment is a config
change, not a code change.

---

## ✨ Features

- 🚀 **One-call setup** — tracing, metrics and logging via a single `AddTelemetry()`.
- ⚙️ **Configuration-driven** — every option bindable from `appsettings.json`,
  environment variables or any other `IConfiguration` source; override in code when you need to.
- 🔋 **Batteries included** — ASP.NET Core, `HttpClient` and .NET runtime
  instrumentation out of the box, each individually toggleable.
- 🛡️ **Sensible defaults** — parent-based sampling, exceptions recorded on spans,
  `/health` excluded from tracing, endpoint validated at startup.
- 🌍 **Runs everywhere** — ASP.NET Core, worker services, console, WPF, WinForms,
  MAUI, and even .NET Framework via `netstandard2.0`.
- 🔌 **Any OTLP backend** — HTTP/protobuf or gRPC, with custom headers for auth.
- 🧩 **Extensible** — add your own `ActivitySource`s, `Meter`s and database
  instrumentation from config or code.
- 🪶 **No lock-in** — it is plain OpenTelemetry underneath, the whole library is
  two source files, and you can drop it at any time.

---

## 🔭 Works with your backend

Anything that speaks [OTLP](https://opentelemetry.io/docs/specs/otlp/) works.
These backends ship with a ready-to-run start script and launch profile in this
repository — see [Running Locally with a Backend](#-running-locally-with-a-backend):

| Backend | Protocol used in the sample |
|---|---|
| [.NET Aspire Dashboard](https://aspire.dev/dashboard/standalone/) | gRPC + API key |
| [Jaeger](https://www.jaegertracing.io/) | gRPC |
| [OpenObserve](https://openobserve.ai/) | HTTP/protobuf or gRPC |
| [SigNoz](https://signoz.io/) | HTTP/protobuf |
| [Grafana](https://grafana.com/) / [Loki](https://grafana.com/oss/loki/) | HTTP/protobuf |
| [OpenSearch Dashboards](https://opensearch.org/) | HTTP/protobuf |

Using a hosted service (Grafana Cloud, Honeycomb, New Relic, Elastic, Datadog, …)?
Point `Endpoint` at its OTLP ingest URL and put the API key in `Headers`.

---

## ✅ Requirements

**Every app type works** — ASP.NET Core, WPF, WinForms, console, worker services,
MAUI/WinUI, UWP. The package ships three targets and NuGet picks the best match
for your **target framework**; the app type plays no role:

| Your target framework | Build you get | Bundles ASP.NET Core instrumentation |
|---|---|---|
| `net10.0` — including `net10.0-windows` (WPF/WinForms) | `net10.0` | ✅ |
| `net8.0`, `net9.0` — including their `-windows` variants | `net8.0` | ✅ |
| .NET Framework 4.6.1+, .NET Standard 2.0 consumers | `netstandard2.0` | ❌ |

The last column is only about the optional
[ASP.NET Core instrumentation](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore),
which traces *incoming HTTP requests* and is therefore meaningless outside a web
app. It is left out of the `netstandard2.0` build so .NET Framework clients don't
drag in an ASP.NET Core dependency they can't use. Everything else — OTLP export,
tracing, metrics, logging, `HttpClient` and runtime instrumentation — is
identical in all three builds.

> A WPF app on `net10.0-windows` therefore gets the full `net10.0` build. Only
> WPF on **.NET Framework** falls back to `netstandard2.0`.

You also need an **OTLP-compatible backend** to receive the telemetry — see
[Works with your backend](#-works-with-your-backend).

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
  straight into your project and own them outright:
  [`TelemetryServiceCollectionExtensions.cs`](./src/OpenTelemetryExtension.Configuration/TelemetryServiceCollectionExtensions.cs)
  and
  [`TelemetryOptions.cs`](./src/OpenTelemetryExtension.Configuration/TelemetryOptions.cs).
- **As a git submodule** — pin the source at a specific commit:
  ```bash
  git submodule add https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration.git external/OpenTelemetryExtension.Configuration
  ```
  then add a `<ProjectReference>` to
  `external/OpenTelemetryExtension.Configuration/src/OpenTelemetryExtension.Configuration/OpenTelemetryExtension.Configuration.csproj`.
- **As a fork** — adapt it to your needs, and open a pull request back upstream
  if your changes are generally useful. See [Contributing](#-contributing).

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

### 3. Start a backend

No backend yet? The .NET Aspire Dashboard is a single container:

```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 -p 4318:18890 -e DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS=true mcr.microsoft.com/aspire/dashboard:latest
```

Run your app, then open <http://localhost:18888> — traces, metrics and logs
appear as soon as the app handles its first request.

---

## ⚙️ Configuration

All keys live under the **`Telemetry`** section. Only `Endpoint` is required; everything
else has a default and telemetry is on out of the box. Bind from `appsettings.json` or set
the same properties in the `AddTelemetry(o => …)` callback.

### Options reference

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
| `EnableAspNetCoreInstrumentation` | Incoming [ASP.NET Core](https://www.nuget.org/packages/OpenTelemetry.Instrumentation.AspNetCore) requests *(net8.0/net10.0 builds; no-op on netstandard2.0)*. | `bool` | `true` | `false` |
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

### Endpoint & protocol

Give `Endpoint` the *base* URL, not a signal path:

- `HttpProtobuf` (default, port `4318`) — the per-signal path (`/v1/traces`,
  `/v1/metrics`, `/v1/logs`) is appended for you.
- `Grpc` (port `4317`) — every signal goes to the base endpoint as-is.

`Endpoint` is validated at registration and `AddTelemetry()` throws when it is
missing. The one exception is `Enabled: false`: validation is skipped, nothing is
registered, and the call becomes a no-op — so you can switch telemetry off in an
environment without also having to supply an endpoint.

### Custom section name

The section defaults to `Telemetry`, but you can bind any section by passing its name:

```csharp
builder.Services.AddTelemetry(builder.Configuration, "MyTelemetry");
// or together with a code callback:
builder.Services.AddTelemetry(builder.Configuration, o => { /* ... */ }, "MyTelemetry");
```

---

## 🧩 Code Configuration

Configure entirely in code instead of `appsettings.json`:

```csharp
builder.Services.AddTelemetry(o =>
{
    o.Endpoint           = new Uri("http://localhost:4318");
    o.ServiceName        = "my-api";
    o.ResourceAttributes = new() { ["deployment.environment"] = "production" };
    o.SampleRatio        = 0.1;

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
});
```

### Registering your own Sources & Meters

The built-in instrumentation (ASP.NET Core, `HttpClient`, .NET runtime) is wired
up automatically. The three `Configure*` callbacks are the extension points for
the signals **your own** application emits, plus anything that needs an extra
NuGet package — such as [databases](#databases).

| Hook | Builder | Used to register |
|---|---|---|
| `ConfigureTracing` | [`TracerProviderBuilder`](https://opentelemetry.io/docs/languages/net/instrumentation/#traces) | **Activity Sources** via `AddSource("Name")` |
| `ConfigureMetrics` | [`MeterProviderBuilder`](https://opentelemetry.io/docs/languages/net/instrumentation/#metrics) | **Meters** via `AddMeter("Name")` |
| `ConfigureLogging` | [`ILoggingBuilder`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.logging.iloggingbuilder) | extra logging providers, filters, etc. |

**Custom metrics.** A [`Meter`](https://learn.microsoft.com/dotnet/api/system.diagnostics.metrics.meter)
creates instruments (counters, histograms, gauges). OpenTelemetry only collects
meters whose name you registered:

```csharp
private static readonly Meter Meter = new("MyApp.Orders");
private static readonly Counter<long> OrdersPlaced = Meter.CreateCounter<long>("orders.placed");

OrdersPlaced.Add(1);

o.ConfigureMetrics = metrics => metrics.AddMeter("MyApp.Orders");
```

**Custom spans.** An [`ActivitySource`](https://learn.microsoft.com/dotnet/api/system.diagnostics.activitysource)
is the tracing equivalent — it creates `Activity` objects (= spans):

```csharp
private static readonly ActivitySource Activity = new("MyApp");

using var span = Activity.StartActivity("ProcessOrder");

o.ConfigureTracing = tracing => tracing.AddSource("MyApp");
```

> The string passed to `AddMeter`/`AddSource` must **exactly match** the name you
> gave the `Meter`/`ActivitySource`. Prefer config? `AdditionalMeters` and
> `AdditionalTracingSources` do the same from `appsettings.json`.

### Databases

Database instrumentation is **not** built in — it depends on your driver, so the
package stays free of database-specific dependencies. There are two kinds:

| Kind | Examples | How to enable |
|---|---|---|
| **Package-based** — ships an `Add…Instrumentation()` extension | SQL Server (`OpenTelemetry.Instrumentation.SqlClient`), EF Core (`OpenTelemetry.Instrumentation.EntityFrameworkCore`) | install the package + call it in `ConfigureTracing` |
| **Source-based** — the driver already emits an `ActivitySource` | Npgsql (`Npgsql`), MySqlConnector (`MySqlConnector`), Oracle (`Oracle.ManagedDataAccess.Core`) | register the source name — code *or* config, no extra package |

`ConfigureTracing` is a single delegate, so chain everything in one assignment:

```csharp
// dotnet add package OpenTelemetry.Instrumentation.SqlClient
// dotnet add package OpenTelemetry.Instrumentation.EntityFrameworkCore
o.ConfigureTracing = t => t
    .AddSqlClientInstrumentation()
    .AddEntityFrameworkCoreInstrumentation()
    .AddSource("Npgsql")            // PostgreSQL
    .AddSource("MySqlConnector");   // MySQL
```

Source-based drivers need no C# at all:

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:4318",
    "AdditionalTracingSources": [ "Npgsql", "MyApp" ],
    "AdditionalMeters": [ "MyApp.Orders" ]
  }
}
```

**Toggling SQL instrumentation from `appsettings.json`.** Because the SqlClient
package is optional, add a custom key and read it in the callback:

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:4318",
    "EnableSqlClientInstrumentation": true
  }
}
```

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

---

## 🖥️ Using outside the Generic Host

`AddTelemetry()` works with **any** `IServiceCollection` — ASP.NET Core, WPF,
WinForms, console, MAUI/WinUI, UWP, worker services, etc.

**With the Generic Host** (recommended for desktop/console), the providers start
and flush automatically:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTelemetry(builder.Configuration);
using var host = builder.Build();
await host.RunAsync();   // telemetry starts here and flushes on shutdown
```

**Without a host** (e.g. a bare `ServiceCollection`), build the provider and
**dispose it on exit** so buffered telemetry is flushed:

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

---

## 🧪 Samples

Two runnable samples live under [`src/`](./src):

| Sample | Project | Demonstrates |
|---|---|---|
| **Web API** | [`…Sample.WebApi`](./src/OpenTelemetryExtension.Configuration.Sample.WebApi) | ASP.NET Core minimal API configured from `appsettings.json`, ready-to-run backend profiles, EF Core and opt-in SQL instrumentation. |
| **WPF** | [`…Sample.Wpf`](./src/OpenTelemetryExtension.Configuration.Sample.Wpf) | Desktop app wiring `AddTelemetry()` through the **Generic Host**, emitting a custom `ActivitySource`/`Meter` and an `HttpClient` span on a button click. |

The WPF sample exports to `http://localhost:4318` by default — point it at any
of the backends below.

---

## 🔌 Running Locally with a Backend

The [Web API sample](./src/OpenTelemetryExtension.Configuration.Sample.WebApi)
ships a start script (Docker Compose or Helm) in [`infrastructure/`](./infrastructure)
and a matching launch profile for each backend:

1. **Start the backend** — run the script from the table. Docker scripts need
   Docker; Helm scripts need a local Kubernetes cluster (e.g. k3s in WSL2).
2. **Run the sample** with the matching profile:
   ```bash
   cd src/OpenTelemetryExtension.Configuration.Sample.WebApi
   dotnet run --launch-profile "Start Aspire"
   ```
3. **Generate traffic** — Swagger opens at `https://localhost:5073/swagger`; call an endpoint.
4. **Open the backend UI** and explore the telemetry.

| Backend | Start infrastructure | Launch profile | Backend UI |
|---|---|---|---|
| .NET Aspire Dashboard | `docker/docker-install-aspire-dashboard.cmd` *(or `helm/helm-install-aspire-dashboard.cmd`)* | `Start Aspire` | <http://localhost:31888> |
| Jaeger | `docker/docker-install-jaeger.cmd` | `Start Jaeger` | <http://localhost:16686> |
| OpenObserve | `helm/helm-install-openobserve.cmd` *(or `docker/docker-install-openobserve.cmd`)* | `Start OpenObserve Http` / `Start OpenObserve Grpc` | <http://localhost:30117> (`admin@web.de`/`admin`) |
| SigNoz | `helm/helm-install-signoz.cmd` | `Start SigNoz` | <http://localhost:30111> (`admin@web.de`) |
| Grafana / Loki | `docker/docker-install-loki.cmd` | `Start Loki` | <http://localhost:3000> (`admin`/`admin`) |
| OpenSearch Dashboards | `docker/docker-install-opensearch.cmd` | `Start OpenSearch` | <http://localhost:5601> |

*(Paths are relative to [`infrastructure/`](./infrastructure).)*

### Sample backend configurations

These are the exact `appsettings.<env>.json` files used by the sample's launch profiles.

#### .NET Aspire Dashboard — `appsettings.aspire.json`

The dashboard requires an API key on the OTLP endpoint (`x-otlp-api-key`). The
gRPC endpoint is exposed on NodePort `31889` (Helm) or host port `31889` (Docker).
Open <http://localhost:31888> and use the **Structured**, **Traces** or
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

#### Jaeger — `appsettings.jaeger.json`

```json
{
  "Telemetry": {
    "Protocol": "Grpc",
    "Endpoint": "http://localhost:4317"
  }
}
```

![Traces from the sample app in the Jaeger UI](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/Jaeger.webp)

#### OpenObserve — HTTP/protobuf — `appsettings.openobserve-http.json`

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

---

## ❓ FAQ

**Does it hide OpenTelemetry from me?**
No. It calls the official OpenTelemetry .NET SDK and exposes the underlying
builders through `ConfigureTracing`, `ConfigureMetrics` and `ConfigureLogging`.
Anything the SDK can do, you can still do.

**How do I turn telemetry off in tests or locally?**
Set `"Enabled": false` (or the environment variable `Telemetry__Enabled=false`).
Nothing is registered and no endpoint is required.

**Does it work with .NET Framework?**
Yes, through the `netstandard2.0` build (.NET Framework 4.6.1+). Only the ASP.NET
Core instrumentation is absent there. See [Requirements](#-requirements).

**Can I use it with .NET Aspire?**
Yes. Point `Endpoint` at the Aspire Dashboard's OTLP endpoint, or use it in
projects that are not part of an Aspire AppHost but should report to the same
dashboard.

**Why is my custom metric or span missing?**
Its `Meter`/`ActivitySource` name is not registered. Add it to
`AdditionalMeters`/`AdditionalTracingSources` or call `AddMeter`/`AddSource` —
the names must match exactly.

---

## 🤝 Contributing

Contributions are welcome — bug fixes, new instrumentation options, documentation
improvements and ideas alike. For anything beyond a small fix, please open an
issue first so we can agree on the approach.

Fork the repo, branch off `main` (`feature/<name>` or `fix/<name>`), add or update
tests for any public API change, and open a PR against `main`:

```bash
dotnet build OpenTelemetryExtension.slnx -c Release
dotnet test  src/OpenTelemetryExtension.Configuration.Tests -c Release
```

Full details — code style, the integration-test stack and the release process —
are in [CONTRIBUTING.md](./CONTRIBUTING.md). Versioning and release notes are
handled at release time, so you don't need to touch them in a feature PR.

If this package saves you time, a ⭐ on
[GitHub](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration)
helps others find it.

---

## 🐛 Report a Bug

Found something broken? Please
[open an issue](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/issues/new/choose)
and include the package version, your target framework, the relevant `Telemetry`
configuration, the OTLP backend you export to, and what you expected versus what
happened. A minimal repro or stack trace speeds things up a lot.

For security-sensitive reports, please follow [SECURITY.md](./SECURITY.md)
instead of opening a public issue.

---

## 📄 License

Released under the [MIT License](./LICENSE).
