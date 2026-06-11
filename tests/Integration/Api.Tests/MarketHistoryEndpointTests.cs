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
            Assert.True(d.TryGetProperty("taxableAmountPerUnit", out _));
            Assert.True(d.TryGetProperty("capitalReturnAmountPerUnit", out _));
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

    // El seed incluye DailySnapshots a 5, 20, 50, 110, 220 y 400 días atrás.
    // Si el mapeo período→días es correcto, cada período devuelve un subconjunto distinto:
    //   1m (30d)  → 5, 20                    = 2 puntos
    //   3m (90d)  → 5, 20, 50                = 3 puntos
    //   6m (180d) → 5, 20, 50, 110           = 4 puntos
    //   1y (365d) → 5, 20, 50, 110, 220      = 5 puntos  (400d queda fuera)
    // Si todos los períodos mapearan a 90 días (bug original), 6m y 1y devolverían 3 puntos
    // igual que 3m y el test fallaría.
    [Theory]
    [InlineData("1m", 2)]
    [InlineData("3m", 3)]
    [InlineData("6m", 4)]
    [InlineData("1y", 5)]
    public async Task GetHistory_Period_ReturnsCorrectNumberOfDataPoints(string period, int expectedCount)
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync($"/api/v1/market/fibras/FUNO11/history?period={period}");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var priceHistory = doc.RootElement.GetProperty("priceHistory").EnumerateArray().ToList();

        Assert.Equal(expectedCount, priceHistory.Count);
    }

    [Theory]
    [InlineData("1m", 30)]
    [InlineData("3m", 90)]
    [InlineData("6m", 180)]
    [InlineData("1y", 365)]
    public async Task GetHistory_Period_AllDatesWithinExpectedRange(string period, int maxDays)
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync($"/api/v1/market/fibras/FUNO11/history?period={period}");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var priceHistory = doc.RootElement.GetProperty("priceHistory").EnumerateArray().ToList();

        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-maxDays));
        Assert.All(priceHistory, point =>
        {
            var date = DateOnly.Parse(point.GetProperty("date").GetString()!);
            Assert.True(date >= cutoff,
                $"Período {period}: fecha {date} es anterior al corte {cutoff} ({maxDays} días)");
        });
    }

    [Fact]
    public async Task GetHistory_LongerPeriodReturnsMorePointsThanShorter()
    {
        await EnsureSeededAsync();

        async Task<int> Count(string period)
        {
            var r = await _client.GetAsync($"/api/v1/market/fibras/FUNO11/history?period={period}");
            var body = await r.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("priceHistory").EnumerateArray().Count();
        }

        var count1m = await Count("1m");
        var count3m = await Count("3m");
        var count6m = await Count("6m");
        var count1y  = await Count("1y");

        Assert.True(count1m < count3m,  $"1m ({count1m}) debe ser < 3m ({count3m})");
        Assert.True(count3m < count6m,  $"3m ({count3m}) debe ser < 6m ({count6m})");
        Assert.True(count6m < count1y,  $"6m ({count6m}) debe ser < 1y ({count1y})");
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
