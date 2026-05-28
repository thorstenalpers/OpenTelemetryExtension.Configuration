# OpenTelemetryExtension.Configuration

[![.NET Standard 2.1](https://img.shields.io/badge/.NET%20Standard-2.1-blue)](#)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)
[![NuGet Version](https://img.shields.io/nuget/v/OpenTelemetryExtension.Configuration.svg)](https://www.nuget.org/packages/OpenTelemetryExtension.Configuration)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OpenTelemetryExtension.Configuration.svg)](https://www.nuget.org/packages/OpenTelemetryExtension.Configuration)
[![Coverage Status](https://coveralls.io/repos/github/thorstenalpers/OpenTelemetryExtension.Configuration/badge.svg?branch=develop)](https://coveralls.io/github/thorstenalpers/OpenTelemetryExtension.Configuration?branch=develop)
[![CI Tests](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/actions/workflows/ci.yml/badge.svg)](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/actions/workflows/ci.yml)
[![Star this repo](https://img.shields.io/github/stars/thorstenalpers/OpenTelemetryExtension.Configuration.svg?style=social&label=Star&maxAge=60)](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration)

An opinionated, zero-boilerplate OpenTelemetry setup for ASP.NET Core — tracing, metrics and logging with a single `AddTelemetry()` call.

---

## ⭐ Features

* **Tracing:** ASP.NET Core, HttpClient and SqlClient instrumentation out of the box.
* **Metrics:** ASP.NET Core, HttpClient and .NET runtime metrics.
* **Logging:** Structured log export via OpenTelemetry Protocol (OTLP).
* **Flexible configuration:** Bind from `appsettings.json` or configure inline in code.
* **Extensible:** Register custom instrumentation via `ConfigureTracing`, `ConfigureMetrics` and `ConfigureLogging` callbacks.
* **Protocol support:** HTTP/protobuf (port 4318) and gRPC (port 4317).

---

## 🚀 Getting Started

### Installation

Install via NuGet:

```shell
dotnet add package OpenTelemetryExtension.Configuration
```

### Register in Service Collection

#### Option A — via `appsettings.json`

```csharp
builder.Services.AddTelemetry(builder.Configuration);
```

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:4318",
    "ServiceName": "my-api",
    "EnvironmentName": "production"
  }
}
```

#### Option B — inline in code

```csharp
builder.Services.AddTelemetry(o =>
{
    o.Endpoint        = new Uri("http://localhost:4318");
    o.ServiceName     = "my-api";
    o.EnvironmentName = "production";
});
```

---

## ⚙️ Configuration Reference

All options can be set via `appsettings.json` under the `Telemetry` key.

| Property | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `true` | Disables all telemetry when `false`. |
| `Endpoint` | `Uri` | — *(required)* | OTLP collector endpoint, e.g. `http://localhost:4318`. |
| `Headers` | `string` | `""` | Exporter headers, e.g. `Authorization=Basic ...`. Format: `key1=value1,key2=value2`. |
| `Protocol` | `string` | `Grpc` | OTLP protocol. Valid values: `HttpProtobuf`, `Grpc`. |
| `ServiceName` | `string?` | `null` | Service name reported to the backend. |
| `EnvironmentName` | `string?` | `null` | Reported as `deployment.environment` resource attribute. |
| `EnableTracing` | `bool` | `true` | Enables distributed tracing. |
| `EnableMetrics` | `bool` | `true` | Enables metrics collection. |
| `EnableLogging` | `bool` | `true` | Enables log export. |
| `EnableAspNetCoreInstrumentation` | `bool` | `true` | Instruments incoming HTTP requests. |
| `EnableHttpClientInstrumentation` | `bool` | `true` | Instruments outgoing `HttpClient` requests. |
| `EnableSqlClientInstrumentation` | `bool` | `true` | Instruments SQL database calls. |
| `EnableRuntimeInstrumentation` | `bool` | `true` | Collects GC, memory and thread pool metrics. |
| `RecordExceptions` | `bool` | `true` | Records exceptions with stack traces on spans. |
| `ExcludeHealthChecks` | `bool` | `true` | Excludes `/health` endpoints from tracing. |

> **Note:** `ConfigureTracing`, `ConfigureMetrics` and `ConfigureLogging` callbacks are only available when configuring inline in code — they cannot be set via `appsettings.json`.

---

## 🔌 Custom Instrumentation

Register additional instrumentation libraries via the callbacks:

```csharp
builder.Services.AddTelemetry(o =>
{
    o.Endpoint = new Uri("http://localhost:4318");

    // e.g. MySQL, Redis, MongoDB, ...
    o.ConfigureTracing = tracing => tracing.AddSource("MyApp");
    o.ConfigureMetrics = metrics => metrics.AddMeter("MyApp");
    o.ConfigureLogging = logging => logging.AddConsole();
});
```

---

## 📡 Backend Examples

Run the helm charts or use docker deploy with the cmd scripts.
See the [deploy folder](./deploy) for all configuration files and startup scripts,
and the [sample project](./OpenTelemetryExtension.Configuration.Sample) for the corresponding `appsettings` configurations.

### .NET Aspire Dashboard

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:18888",
    "Protocol": "HttpProtobuf"
  }
}
```

### Jaeger

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:4318",
    "Protocol": "HttpProtobuf"
  }
}
```

### SigNoz

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:50709"
  }
}
```

### OpenSearch

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:30318",
    "Protocol": "HttpProtobuf"
  }
}
```

### Grafana Loki

> Loki supports OTLP for **logs only**. Tracing and metrics must be disabled.

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:3100/otlp",
    "Protocol": "HttpProtobuf",
    "EnableTracing": false,
    "EnableMetrics": false
  }
}
```

### OpenObserve — HTTP/protobuf

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:30117/api/default",
    "Protocol": "HttpProtobuf",
    "Headers": "Authorization=Basic <base64>,stream-name=default"
  }
}
```

### OpenObserve — gRPC

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:30118",
    "Protocol": "Grpc",
    "Headers": "Authorization=Basic <base64>,organization=default,stream-name=default"
  }
}
```

---

## 🤝 How to Contribute

Contributions are welcome! If you'd like to improve the project, please:

1. Check out our [contributing guidelines](CONTRIBUTING.md).
2. Ideally, open an issue before starting work.
3. Submit a pull request with your changes.

Thank you for helping make OpenTelemetryExtension.Configuration better!

---

## 🐞 Report a Bug

If you encounter any issues or bugs, please [report them here](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/issues).

---

For additional licensing and attribution details, see [NOTICE.md](./NOTICE.md) and [THIRD_PARTY_LICENSES.md](./THIRD_PARTY_LICENSES.md).
