namespace OpenTelemetryExtension.Configuration.IntegrationTests.Utils;

internal static class IntegrationConfig
{
    public static string OpenObserveBaseUrl => "http://localhost:30117/api/default";
    public static string OpenObserveUser => "admin@web.de";
    public static string OpenObservePassword => "admin";
    public static string OtlpHeaders => "Authorization=Basic YWRtaW5Ad2ViLmRlOmFkbWlu,stream-name=default";
    public static Uri OtlpEndpoint => new(OpenObserveBaseUrl);
    public static string SqlConnectionString => "Server=localhost,31433;Database=master;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True;Encrypt=False";
}
