using System.Net;
using System.Text.Json;

namespace Api.Tests;

public class MarketSnapshotsEndpointTests(ApiWebFactory factory)
    : IClassFixture<ApiWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static bool _seeded;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private async Task EnsureSeededAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!_seeded)
            {
                await factory.SeedMarketAsync();
                _seeded = true;
            }
        }
        finally { _lock.Release(); }
    }

    [Fact]
    public async Task GetSnapshots_ReturnsOk_WithJsonArray()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/snapshots");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task GetSnapshots_IncludesAllActiveFibras()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/snapshots");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var tickers = doc.RootElement.EnumerateArray()
            .Select(e => e.GetProperty("ticker").GetString())
            .ToList();

        Assert.Contains("FUNO11", tickers);
        Assert.Contains("DANHOS13", tickers);
        Assert.True(tickers.Count >= 2);
    }

    [Fact]
    public async Task GetSnapshots_EachItemHasRequiredFields()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/snapshots");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var first = doc.RootElement.EnumerateArray().First();

        Assert.True(first.TryGetProperty("fibraId", out _));
        Assert.True(first.TryGetProperty("ticker", out _));
        Assert.True(first.TryGetProperty("lastPrice", out _));
        Assert.True(first.TryGetProperty("dailyChange", out _));
        Assert.True(first.TryGetProperty("dailyChangePct", out _));
        Assert.True(first.TryGetProperty("volume", out _));
        Assert.True(first.TryGetProperty("week52High", out _));
        Assert.True(first.TryGetProperty("week52Low", out _));
        Assert.True(first.TryGetProperty("capturedAt", out _));
        Assert.True(first.TryGetProperty("freshnessStatus", out _));
    }

    [Fact]
    public async Task GetSnapshots_FibraWithSnapshot_HasPriceAndNonNullFreshness()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/snapshots");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var funo = doc.RootElement.EnumerateArray()
            .First(e => e.GetProperty("ticker").GetString() == "FUNO11");

        Assert.Equal(JsonValueKind.Number, funo.GetProperty("lastPrice").ValueKind);
        Assert.True(funo.GetProperty("lastPrice").GetDecimal() > 0);
        Assert.NotEqual(JsonValueKind.Null, funo.GetProperty("freshnessStatus").ValueKind);
    }

    [Fact]
    public async Task GetSnapshots_FibraWithoutSnapshot_HasNullFreshnessStatus()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/snapshots");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        // DANHOS13 no tiene snapshot seeded — freshnessStatus debe ser null
        var danhos = doc.RootElement.EnumerateArray()
            .First(e => e.GetProperty("ticker").GetString() == "DANHOS13");

        Assert.Equal(JsonValueKind.Null, danhos.GetProperty("freshnessStatus").ValueKind);
        Assert.Equal(JsonValueKind.Null, danhos.GetProperty("lastPrice").ValueKind);
    }

    [Fact]
    public async Task GetSnapshots_FreshnessStatusIsValidValue()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/snapshots");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        var validStatuses = new[] { "fresh", "stale", "off-hours", "critical" };

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            var status = item.GetProperty("freshnessStatus");
            if (status.ValueKind == JsonValueKind.String)
                Assert.Contains(status.GetString(), validStatuses);
        }
    }
}
