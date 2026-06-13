using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace OpenTelemetryExtension.Configuration.IntegrationTests.Utils;

internal sealed class SigNozClient : IDisposable
{
    private const long OneHourMillis = 3_600_000L;

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private string? _jwt;

    public Task<long> CountServiceSignalAsync(string dataSource, string serviceName, TimeSpan timeout, CancellationToken ct = default)
        => PollUntilAsync(BuildServiceQuery(dataSource, serviceName), timeout, ct);

    public Task<long> CountMetricAsync(string metricName, TimeSpan timeout, CancellationToken ct = default)
        => PollUntilAsync(BuildMetricQuery(metricName), timeout, ct);

    private async Task<long> PollUntilAsync(Func<long, long, object> buildQuery, TimeSpan timeout, CancellationToken ct)
    {
        await EnsureAuthenticatedAsync(ct);

        var deadline = DateTime.UtcNow + timeout;
        long count = 0;

        while (DateTime.UtcNow < deadline)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            count = await QueryAsync(buildQuery(now - OneHourMillis, now), ct);
            if (count > 0)
            {
                return count;
            }

            await Task.Delay(2000, ct);
        }

        return count;
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (_jwt is not null)
        {
            return;
        }

        var orgId = await GetOrgIdAsync(ct);

        var payload = new { email = IntegrationConfig.SigNozUser, password = IntegrationConfig.SigNozPassword, orgId };
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync($"{IntegrationConfig.SigNozApiBaseUrl}/api/v2/sessions/email_password", content, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        _jwt = doc.RootElement.GetProperty("data").GetProperty("accessToken").GetString();
    }

    private async Task<string> GetOrgIdAsync(CancellationToken ct)
    {
        var url = $"{IntegrationConfig.SigNozApiBaseUrl}/api/v2/sessions/context?email={Uri.EscapeDataString(IntegrationConfig.SigNozUser)}";
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        var orgs = doc.RootElement.GetProperty("data").GetProperty("orgs");
        if (orgs.ValueKind != JsonValueKind.Array || orgs.GetArrayLength() == 0)
        {
            throw new InvalidOperationException($"SigNoz returned no organization for user '{IntegrationConfig.SigNozUser}'.");
        }

        return orgs[0].GetProperty("id").GetString()
            ?? throw new InvalidOperationException("SigNoz organization id was null.");
    }

    private async Task<long> QueryAsync(object query, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{IntegrationConfig.SigNozApiBaseUrl}/api/v4/query_range")
        {
            Content = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _jwt);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            return 0;
        }

        return ParseCount(await response.Content.ReadAsStringAsync(ct));
    }

    private static Func<long, long, object> BuildServiceQuery(string dataSource, string serviceName) => (start, end) => new
    {
        start,
        end,
        step = 60,
        compositeQuery = new
        {
            queryType = "builder",
            panelType = "table",
            builderQueries = new Dictionary<string, object>
            {
                ["A"] = new
                {
                    dataSource,
                    queryName = "A",
                    expression = "A",
                    aggregateOperator = "count",
                    timeAggregation = "count",
                    spaceAggregation = "sum",
                    stepInterval = 60,
                    disabled = false,
                    reduceTo = "sum",
                    filters = new
                    {
                        op = "AND",
                        items = new[]
                        {
                            new
                            {
                                key = new { key = "service.name", dataType = "string", type = "resource", isColumn = false },
                                op = "=",
                                value = serviceName,
                            },
                        },
                    },
                },
            },
        },
    };

    private static Func<long, long, object> BuildMetricQuery(string metricName) => (start, end) => new
    {
        start,
        end,
        step = 60,
        compositeQuery = new
        {
            queryType = "builder",
            panelType = "table",
            builderQueries = new Dictionary<string, object>
            {
                ["A"] = new
                {
                    dataSource = "metrics",
                    queryName = "A",
                    expression = "A",
                    aggregateOperator = "count",
                    timeAggregation = "count",
                    spaceAggregation = "sum",
                    stepInterval = 60,
                    disabled = false,
                    reduceTo = "sum",
                    aggregateAttribute = new { key = metricName, dataType = "float64", type = "" },
                    filters = new { op = "AND", items = Array.Empty<object>() },
                },
            },
        },
    };

    private static long ParseCount(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data)
            || !data.TryGetProperty("result", out var result)
            || result.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        long max = 0;
        foreach (var query in result.EnumerateArray())
        {
            if (!query.TryGetProperty("series", out var series) || series.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var serie in series.EnumerateArray())
            {
                if (!serie.TryGetProperty("values", out var values) || values.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var point in values.EnumerateArray())
                {
                    if (point.TryGetProperty("value", out var value)
                        && TryReadDouble(value, out var parsed)
                        && parsed > max)
                    {
                        max = (long)parsed;
                    }
                }
            }
        }

        return max;
    }

    private static bool TryReadDouble(JsonElement element, out double value)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Number:
                return element.TryGetDouble(out value);
            case JsonValueKind.String:
                return double.TryParse(element.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            default:
                value = 0;
                return false;
        }
    }

    public void Dispose() => _http.Dispose();
}
