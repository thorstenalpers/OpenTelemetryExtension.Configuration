using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace OpenTelemetryExtension.Configuration.IntegrationTests.Utils;

internal sealed class OtelTestHost : IDisposable
{
    private readonly ServiceProvider _provider;

    public OtelTestHost(Action<TelemetryOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddTelemetry(opt =>
        {
            opt.Protocol = OtlpExportProtocol.HttpProtobuf;
            opt.Endpoint = IntegrationConfig.OtlpEndpoint;
            opt.Headers = IntegrationConfig.OtlpHeaders;
            configure(opt);
        });

        _provider = services.BuildServiceProvider();
    }

    public ILogger<T> CreateLogger<T>() => _provider.GetRequiredService<ILogger<T>>();

    public void Flush()
    {
        _provider.GetService<TracerProvider>()?.ForceFlush(15_000);
        _provider.GetService<MeterProvider>()?.ForceFlush(15_000);
        _provider.GetService<LoggerProvider>()?.ForceFlush(15_000);

        Thread.Sleep(500);
    }

    public void Dispose()
    {
        Flush();
        _provider.Dispose();
    }
}
