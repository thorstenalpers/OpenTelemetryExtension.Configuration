using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using OpenTelemetryExtension.Configuration.IntegrationTests.Utils;

namespace OpenTelemetryExtension.Configuration.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class TelemetryIntegrationTests
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(40);

    [Fact]
    public async Task Traces_AreExported_AndQueryableInOpenObserve()
    {
        var runId = Guid.NewGuid().ToString("N");
        var serviceName = $"itest-traces-{runId}";
        var sourceName = $"Itest.Traces.{runId}";

        using (var host = new OtelTestHost(o =>
        {
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

        using var client = new OpenObserveClient();
        var count = await client.PollUntilAsync(
            "traces",
            $"SELECT COUNT(*) AS c FROM \"default\" WHERE service_name = '{serviceName}'",
            QueryTimeout);

        Assert.True(count > 0, $"Expected at least one trace for service '{serviceName}' in OpenObserve.");
    }

    [Fact]
    public async Task Metrics_AreExported_AndQueryableInOpenObserve()
    {
        var runId = Guid.NewGuid().ToString("N");
        var serviceName = $"itest-metrics-{runId}";
        var meterName = $"Itest.Metrics.{runId}";
        var counterName = $"itest_counter_{runId}"; // becomes the OpenObserve metrics stream name

        using (var host = new OtelTestHost(o =>
        {
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

        using var client = new OpenObserveClient();
        var count = await client.PollUntilAsync(
            "metrics",
            $"SELECT COUNT(*) AS c FROM \"{counterName}\"",
            QueryTimeout);

        Assert.True(count > 0, $"Expected the metrics stream '{counterName}' to contain data points in OpenObserve.");
    }

    [Fact]
    public async Task Logs_AreExported_AndQueryableInOpenObserve()
    {
        var runId = Guid.NewGuid().ToString("N");
        var serviceName = $"itest-logs-{runId}";

        using (var host = new OtelTestHost(o =>
        {
            o.ServiceName = serviceName;
            o.EnableTracing = false;
            o.EnableMetrics = false;
        }))
        {
            var logger = host.CreateLogger<TelemetryIntegrationTests>();
            logger.LogInformation("integration log {RunId}", runId);

            host.Flush();
        }

        using var client = new OpenObserveClient();
        var count = await client.PollUntilAsync(
            "logs",
            $"SELECT COUNT(*) AS c FROM \"default\" WHERE service_name = '{serviceName}'",
            QueryTimeout);

        Assert.True(count > 0, $"Expected at least one log record for service '{serviceName}' in OpenObserve.");
    }
}
