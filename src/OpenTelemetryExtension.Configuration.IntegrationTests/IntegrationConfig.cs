namespace OpenTelemetryExtension.Configuration.IntegrationTests;

internal static class IntegrationConfig
{
    public static string OpenObserveBaseUrl =>
        Environment.GetEnvironmentVariable("OTEL_IT_OPENOBSERVE_URL") ?? "http://localhost:30117/api/default";

    public static string OpenObserveUser =>
        Environment.GetEnvironmentVariable("OTEL_IT_OPENOBSERVE_USER") ?? "admin@web.de";

    public static string OpenObservePassword =>
        Environment.GetEnvironmentVariable("OTEL_IT_OPENOBSERVE_PASSWORD") ?? "admin";

    // OTLP exporter auth + log stream routing. Base64 decodes to "admin@web.de:admin".
    public static string OtlpHeaders =>
        Environment.GetEnvironmentVariable("OTEL_IT_OTLP_HEADERS")
        ?? "Authorization=Basic YWRtaW5Ad2ViLmRlOmFkbWlu,stream-name=default";

    public static Uri OtlpEndpoint => new(OpenObserveBaseUrl);

    public static string SqlConnectionString =>
        Environment.GetEnvironmentVariable("OTEL_IT_SQL_CONNECTION")
        ?? "Server=localhost,31433;Database=master;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;Encrypt=False";

    public static (string Host, int Port) OpenObserveEndpoint => ("localhost", 30117);

    public static (string Host, int Port) SqlServerEndpoint => ("localhost", 31433);
}
