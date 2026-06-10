using Microsoft.Data.SqlClient;
using OpenTelemetry.Trace;
using OpenTelemetryExtension.Configuration.IntegrationTests.Infrastructure;

namespace OpenTelemetryExtension.Configuration.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class SqlServerInstrumentationTests
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(40);

    [SqlIntegrationFact]
    public async Task SqlServer_CommandSpans_AreExported_AndQueryableInOpenObserve()
    {
        var runId = Guid.NewGuid().ToString("N");
        var serviceName = $"itest-sql-{runId}";

        using (var host = new OtelTestHost(o =>
        {
            o.ServiceName = serviceName;
            o.EnableMetrics = false;
            o.EnableLogging = false;
            o.ConfigureTracing = tracing => tracing.AddSqlClientInstrumentation();
        }))
        {
            await using var connection = new SqlConnection(IntegrationConfig.SqlConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT '{runId}' AS run_id";
            _ = await command.ExecuteScalarAsync();

            host.Flush();
        }

        using var client = new OpenObserveClient();

        var spans = await client.PollUntilAsync(
            "traces",
            $"SELECT COUNT(*) AS c FROM \"default\" WHERE service_name = '{serviceName}'",
            QueryTimeout);
        Assert.True(spans > 0, $"Expected a SQL Server span for service '{serviceName}' in OpenObserve.");

        // Instrumentation emits the current database semantic conventions:
        // db.system.name = "microsoft.sql_server" (flattened to db_system_name in OpenObserve).
        var sqlServerSpans = await client.PollUntilAsync(
            "traces",
            $"SELECT COUNT(*) AS c FROM \"default\" WHERE service_name = '{serviceName}' AND db_system_name = 'microsoft.sql_server'",
            TimeSpan.FromSeconds(10));
        Assert.True(sqlServerSpans > 0, $"Expected the SQL Server span to carry db_system_name = 'microsoft.sql_server' for service '{serviceName}'.");
    }
}
