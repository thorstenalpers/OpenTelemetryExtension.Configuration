using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OpenTelemetryExtension.Configuration.IntegrationTests.Utils;

// Thin wrapper around the OpenObserve _search API used to assert that emitted
// telemetry actually arrived. The API needs the root user credentials, not the
// OTLP ingestion passcode.
internal sealed class OpenObserveClient : IDisposable
{
    private const long OneHourMicros = 3_600_000_000L;

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

    public OpenObserveClient()
    {
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{IntegrationConfig.OpenObserveUser}:{IntegrationConfig.OpenObservePassword}"));
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
    }

    public async Task<long> CountAsync(string streamType, string sql, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
        var payload = new { query = new { sql, start_time = now - OneHourMicros, end_time = now, size = 1 } };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync(
            $"{IntegrationConfig.OpenObserveBaseUrl}/_search?type={streamType}", content, ct);

        // A missing stream (no data yet) answers non-2xx; treat it as "nothing there yet".
        if (!response.IsSuccessStatusCode)
        {
            return 0;
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.TryGetProperty("hits", out var hits)
            || hits.ValueKind != JsonValueKind.Array
            || hits.GetArrayLength() == 0)
        {
            return 0;
        }

        return hits[0].TryGetProperty("c", out var c) && c.TryGetInt64(out var value) ? value : 0;
    }

    public async Task<long> PollUntilAsync(string streamType, string sql, TimeSpan timeout, CancellationToken ct = default)
    {
        var deadline = DateTime.UtcNow + timeout;
        long count = 0;

        while (DateTime.UtcNow < deadline)
        {
            count = await CountAsync(streamType, sql, ct);
            if (count > 0)
            {
                return count;
            }

            await Task.Delay(2000, ct);
        }

        return count;
    }

    public void Dispose() => _http.Dispose();
}
