using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetryExtension.Configuration.IntegrationTests.Infrastructure;

// Builds a service provider wired up through the library's AddTelemetry() against
// the OpenObserve OTLP endpoint, and exposes a force-flush so a test can assert
// straight afterwards instead of waiting for the batch interval.
internal sealed class OtelTestHost : IDisposable
{
    private readonly ServiceProvider _provider;

    public OtelTestHost(Action<TelemetryOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddTelemetry(o =>
        {
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
            o.Endpoint = IntegrationConfig.OtlpEndpoint;
            o.Headers = IntegrationConfig.OtlpHeaders;
            o.EnableAspNetCoreInstrumentation = false;
            o.EnableHttpClientInstrumentation = false;
            o.EnableRuntimeInstrumentation = false;
            configure(o);
        });

        _provider = services.BuildServiceProvider();

        // Resolve the providers so the SDK starts listening before telemetry is emitted.
        _ = _provider.GetService<TracerProvider>();
        _ = _provider.GetService<MeterProvider>();
    }

    public ILogger<T> CreateLogger<T>() => _provider.GetRequiredService<ILogger<T>>();

    public void Flush()
    {
        _provider.GetService<TracerProvider>()?.ForceFlush(15_000);
        _provider.GetService<MeterProvider>()?.ForceFlush(15_000);
        _provider.GetService<LoggerProvider>()?.ForceFlush(15_000);
    }

    public void Dispose()
    {
        Flush();
        _provider.Dispose();
    }
}
