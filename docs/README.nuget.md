**Production-ready OpenTelemetry for .NET in one line.** Traces, metrics and logs
are exported over OTLP to Jaeger, Grafana, SigNoz, OpenObserve, the .NET Aspire
Dashboard or any other OpenTelemetry backend — configured from
`appsettings.json`, with sensible defaults and no boilerplate.

## Quick Start

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

## Features

- **One call, three signals** — tracing, metrics and logging via `AddTelemetry()`.
- **Configuration-driven** — every option bindable from `appsettings.json` or environment variables; override in code when needed.
- **Batteries included** — ASP.NET Core, `HttpClient` and .NET runtime instrumentation, each toggleable.
- **Runs everywhere** — ASP.NET Core, worker, console, WPF, WinForms, MAUI and .NET Framework via `netstandard2.0`.
- **No lock-in** — plain OpenTelemetry underneath; any OTLP backend over HTTP/protobuf or gRPC.

## Documentation

- [Configuration reference](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration#-configuration) — every option with its default
- [Backends](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration#-backends) — ready-to-run setups for Aspire, Jaeger, SigNoz, OpenObserve, Grafana/Loki and OpenSearch
- [Samples](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration#-samples) — ASP.NET Core Web API and WPF
- [FAQ](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration#-faq) · [Report a bug](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/issues/new/choose) · [Release notes](https://github.com/thorstenalpers/OpenTelemetryExtension.Configuration/releases)
