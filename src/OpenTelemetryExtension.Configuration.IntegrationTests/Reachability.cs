using System.Net.Sockets;

namespace OpenTelemetryExtension.Configuration.IntegrationTests;

internal static class Reachability
{
    public static bool IsTcpOpen(string host, int port, int timeoutMs = 1500)
    {
        try
        {
            using var client = new TcpClient();
            return client.ConnectAsync(host, port).Wait(timeoutMs) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

    private static bool? _openObserve;
    private static bool? _sqlServer;

    public static bool OpenObserveAvailable =>
        _openObserve ??= IsTcpOpen(IntegrationConfig.OpenObserveEndpoint.Host, IntegrationConfig.OpenObserveEndpoint.Port);

    public static bool SqlServerAvailable =>
        _sqlServer ??= IsTcpOpen(IntegrationConfig.SqlServerEndpoint.Host, IntegrationConfig.SqlServerEndpoint.Port);
}
