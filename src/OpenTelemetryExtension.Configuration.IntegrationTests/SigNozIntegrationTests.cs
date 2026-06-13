using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetryExtension.Configuration.IntegrationTests.Utils;

namespace OpenTelemetryExtension.Configuration.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class SigNozIntegrationTests
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(40);

    [Fact]
    public async Task Traces_AreExported_AndQueryableInSigNoz()
    {
        var runId = Guid.NewGuid().ToString("N");
        var serviceName = $"itest-signoz-traces-{runId}";
        var sourceName = $"Itest.SigNoz.Traces.{runId}";

        using (var host = new OtelTestHost(o =>
        {
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
            o.Endpoint = IntegrationConfig.SigNozOtlpEndpoint;
            o.Headers = string.Empty;
            o.ServiceName = serviceName;
            o.EnableMetrics = false;
            o.EnableLogging = false;
            o.AdditionalTracingSources = [sourceName];
        }))
        {
            using var source = new ActivitySource(sourceName);
            using (var activity = source.StartActivity("integration-span"))
            {
                activity?.SetTag("itest.run", runId);
            }

            host.Flush();
        }

        using var client = new SigNozClient();
        var count = await client.CountServiceSignalAsync("traces", serviceName, QueryTimeout);

        Assert.True(count > 0, $"Expected at least one trace for service '{serviceName}' in SigNoz.");
    }

    [Fact]
    public async Task Metrics_AreExported_AndQueryableInSigNoz()
    {
        var runId = Guid.NewGuid().ToString("N");
        var serviceName = $"itest-signoz-metrics-{runId}";
        var meterName = $"Itest.SigNoz.Metrics.{runId}";
        var counterName = $"itest_signoz_counter_{runId}";

        using (var host = new OtelTestHost(o =>
        {
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
            o.Endpoint = IntegrationConfig.SigNozOtlpEndpoint;
            o.Headers = string.Empty;
            o.ServiceName = serviceName;
            o.EnableTracing = false;
            o.EnableLogging = false;
            o.AdditionalMeters = [meterName];
        }))
        {
            using var meter = new Meter(meterName);
            var counter = meter.CreateCounter<long>(counterName);
            counter.Add(3);

            host.Flush();
        }

        using var client = new SigNozClient();
        var count = await client.CountMetricAsync(counterName, QueryTimeout);

        Assert.True(count > 0, $"Expected the metric '{counterName}' to contain data points in SigNoz.");
    }

    [Fact]
    public async Task Logs_AreExported_AndQueryableInSigNoz()
    {
        var runId = Guid.NewGuid().ToString("N");
        var serviceName = $"itest-signoz-logs-{runId}";

        using (var host = new OtelTestHost(o =>
        {
            o.Protocol = OtlpExportProtocol.HttpProtobuf;
            o.Endpoint = IntegrationConfig.SigNozOtlpEndpoint;
            o.Headers = string.Empty;
            o.ServiceName = serviceName;
            o.EnableTracing = false;
            o.EnableMetrics = false;
        }))
        {
            var logger = host.CreateLogger<SigNozIntegrationTests>();
            logger.LogInformation("integration log {RunId}", runId);

            host.Flush();
        }

        using var client = new SigNozClient();
        var count = await client.CountServiceSignalAsync("logs", serviceName, QueryTimeout);

        Assert.True(count > 0, $"Expected at least one log record for service '{serviceName}' in SigNoz.");
    }
}
