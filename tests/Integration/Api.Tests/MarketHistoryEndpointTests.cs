using System.Net;
using System.Text.Json;

namespace Api.Tests;

public class MarketHistoryEndpointTests(ApiWebFactory factory)
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
    public async Task GetHistory_KnownTicker_ReturnsOk()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/fibras/FUNO11/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_ResponseHasRequiredFields()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/fibras/FUNO11/history");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("ticker", out var ticker));
        Assert.Equal("FUNO11", ticker.GetString());
        Assert.True(root.TryGetProperty("priceHistory", out var priceHistory));
        Assert.Equal(JsonValueKind.Array, priceHistory.ValueKind);
        Assert.True(root.TryGetProperty("distributions", out var distributions));
        Assert.Equal(JsonValueKind.Array, distributions.ValueKind);
        Assert.True(root.TryGetProperty("annualizedYield", out _));
    }

    [Fact]
    public async Task GetHistory_DistributionsContainDateAndAmount()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/fibras/FUNO11/history");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var distributions = doc.RootElement.GetProperty("distributions").EnumerateArray().ToList();

        // FUNO11 tiene distribuciones seeded via HasData — se retornan hasta 8
        Assert.NotEmpty(distributions);
        foreach (var d in distributions)
        {
            Assert.True(d.TryGetProperty("date", out _));
            Assert.True(d.TryGetProperty("amountPerUnit", out var amount));
            Assert.True(amount.GetDecimal() > 0);
        }
    }

    [Fact]
    public async Task GetHistory_WithPriceSnapshot_ReturnsPositiveAnnualizedYield()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/fibras/FUNO11/history");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var annualizedYield = doc.RootElement.GetProperty("annualizedYield");

        Assert.NotEqual(JsonValueKind.Null, annualizedYield.ValueKind);
        Assert.True(annualizedYield.GetDecimal() > 0);
    }

    [Fact]
    public async Task GetHistory_PriceHistoryContainsSeededSnapshot()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/fibras/FUNO11/history");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var priceHistory = doc.RootElement.GetProperty("priceHistory").EnumerateArray().ToList();

        Assert.NotEmpty(priceHistory);
        var first = priceHistory.First();
        Assert.True(first.TryGetProperty("date", out _));
        Assert.True(first.TryGetProperty("close", out var close));
        Assert.True(close.GetDecimal() > 0);
    }

    [Fact]
    public async Task GetHistory_UnknownTicker_Returns404()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/fibras/FAKE99/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("1m")]
    [InlineData("3m")]
    [InlineData("6m")]
    [InlineData("1y")]
    public async Task GetHistory_AllPeriodParams_ReturnOk(string period)
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync($"/api/v1/market/fibras/FUNO11/history?period={period}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_TickerIsCaseInsensitive()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/market/fibras/funo11/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_FibraWithNoDistributions_ReturnsNullYield()
    {
        await EnsureSeededAsync();

        // FINN13 no tiene distribuciones seeded — yield debe ser null
        var response = await _client.GetAsync("/api/v1/market/fibras/FINN13/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var annualizedYield = doc.RootElement.GetProperty("annualizedYield");

        Assert.Equal(JsonValueKind.Null, annualizedYield.ValueKind);
    }
}
