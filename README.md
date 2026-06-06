[![OpenTelemetryExtension.Configuration](https://raw.githubusercontent.com/thorstenalpers/OpenTelemetryExtension.Configuration/main/assets/banner.png)](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration)
---
[![CI](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/actions/workflows/ci.yml/badge.svg)](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/actions/workflows/ci.yml)
[![Coverage Status](https://coveralls.io/repos/github/thorstenalpers/OpenTelemetryExtension.Configuration/badge.svg?branch=develop)](https://coveralls.io/github/thorstenalpers/OpenTelemetryExtension.Configuration?branch=develop)
[![NuGet Version](https://img.shields.io/nuget/v/OpenTelemetryExtension.Configuration.svg)](https://www.nuget.org/packages/OpenTelemetryExtension.Configuration)
[![NuGet Downloads](https://img.shields.io/nuget/dt/OpenTelemetryExtension.Configuration.svg)](https://www.nuget.org/packages/OpenTelemetryExtension.Configuration)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](./LICENSE)

Configurable OpenTelemetry setup for .NET applications providing **tracing, metrics, and logging** via OTLP, configurable through code or `appsettings.json`.

---

## Features

- **One-call setup** — tracing, metrics and logging via a single `AddTelemetry()`, configured from `appsettings.json` or code
- **All three signals over OTLP** — HTTP/protobuf or gRPC, to any OTLP-compatible backend
- **Built-in instrumentation** — ASP.NET Core, `HttpClient`, SQL Client and .NET runtime metrics, each toggleable
- **Sensible defaults** — sampling, health-check path exclusion and exception recording work out of the box
- **Startup validation** — misconfiguration fails fast with a clear error
- **Extensible** — `ConfigureTracing`/`ConfigureMetrics`/`ConfigureLogging` hooks for custom sources, meters and providers
- **Broad target support** — `netstandard2.0` and `net10.0`

---


## 📦 Installation

```bash
dotnet add package OpenTelemetryExtension.Configuration
```

---

## ⚙️ Quick Start

### 1. Register services

```csharp
builder.Services.AddTelemetry(builder.Configuration);
```

---

### 2. Configuration (appsettings.json)

```json
{
  "Telemetry": {
    "Enabled": true,
    "Endpoint": "http://localhost:4318",
    "ServiceName": "my-api",
    "ResourceAttributes": {
      "deployment.environment": "production"
    }
  }
}
```

---

## ✨ Code Configuration (Alternative)

```csharp
builder.Services.AddTelemetry(o =>
{
    o.Endpoint = new Uri("http://localhost:4318");
    o.ServiceName = "my-api";
    o.ResourceAttributes = new() { ["deployment.environment"] = "production" };
    o.SampleRatio = 0.1;
});
```

That's it. Tracing, metrics and logging are all exported via OTLP.

---

## Configuration Reference

All options are set under the `Telemetry` key in `appsettings.json`.

| Property | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `false` | Must be `true` to activate telemetry. |
| `Endpoint` | `Uri` | *(required)* | OTLP collector endpoint, e.g. `http://localhost:4318`. |
| `Headers` | `string` | `""` | Exporter headers. Format: `key1=value1,key2=value2`. |
| `Protocol` | `string` | `HttpProtobuf` | `HttpProtobuf` (port 4318) or `Grpc` (port 4317). |
| `ServiceName` | `string?` | `null` | Service name shown in the backend. |
| `ResourceAttributes` | `object` | `{}` | Additional resource attributes, e.g. `{ "deployment.environment": "production", "team": "backend" }`. |
| `SampleRatio` | `double` | `1.0` | Fraction of traces to sample. `0.1` = 10%, `1.0` = all. |
| `EnableTracing` | `bool` | `true` | Enables distributed tracing. |
| `EnableMetrics` | `bool` | `true` | Enables metrics collection. |
| `EnableLogging` | `bool` | `true` | Enables log export via OTLP. |
| `EnableAspNetCoreInstrumentation` | `bool` | `true` | Instruments incoming HTTP requests. |
| `EnableHttpClientInstrumentation` | `bool` | `true` | Instruments outgoing `HttpClient` requests. |
| `EnableSqlClientInstrumentation` | `bool` | `false` | Instruments SQL calls. Opt-in — not all apps use SQL. |
| `EnableRuntimeInstrumentation` | `bool` | `true` | Collects GC, memory and thread pool metrics. |
| `RecordExceptions` | `bool` | `true` | Records exception stack traces on spans. |
| `ExcludedPaths` | `string[]` | `["/health"]` | Paths excluded from tracing. |
| `IncludeScopes` | `bool` | `true` | Includes log scopes in exported log records. |
| `IncludeFormattedMessage` | `bool` | `true` | Includes the formatted message in exported log records. |

> `ConfigureTracing`, `ConfigureMetrics` and `ConfigureLogging` callbacks are only available when configuring inline in code.

### Production example

```json
{
  "Telemetry": {
    "Enabled":         true,
    "Endpoint":        "http://otel-collector:4318",
    "ServiceName":     "my-api",
    "ResourceAttributes": {
      "deployment.environment": "Stage",
      "team": "backend"
    }
  }
}
```

---

## Configure in Code

```csharp
builder.Services.AddTelemetry(o =>
{
    o.Endpoint        = new Uri("http://localhost:4318");
    o.ServiceName     = "my-api";
    o.ResourceAttributes = new() { ["deployment.environment"] = "production" };
    o.SampleRatio     = 0.1;

    // Register additional instrumentation
    o.ConfigureTracing = tracing => tracing.AddSource("MyApp");
    o.ConfigureMetrics = metrics => metrics.AddMeter("MyApp");
    o.ConfigureLogging = logging => logging.AddConsole();
});
```

---

## Backend Examples

See the [infrastructure folder](./infrastructure) for Docker Compose files and Helm charts, and the [sample project](./src/OpenTelemetryExtension.Configuration.Sample) for full `appsettings` configurations.

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

### Grafana Loki *(logs only)*

```json
{
  "Telemetry": {
    "Endpoint":      "http://localhost:3100/otlp",
    "Protocol":      "HttpProtobuf",
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
    "Headers":  "Authorization=Basic <base64>,stream-name=default"
  }
}
```

### OpenObserve — gRPC

```json
{
  "Telemetry": {
    "Endpoint": "http://localhost:30118",
    "Protocol": "Grpc",
    "Headers":  "Authorization=Basic <base64>,organization=default,stream-name=default"
  }
}
```

---

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md).

## Report a Bug

[Open an issue](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/issues).
