using System.Net;
using System.Text.Json;

namespace Api.Tests;

public class CompareEndpointTests(ApiWebFactory factory)
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
                await factory.SeedCompareAsync();
                _seeded = true;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    [Fact]
    public async Task GetCompare_ReturnsOk_WithThreeFibraColumns_AndNestedBlocks()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/compare?tickers=FUNO11,FMTY14,TERRA13");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.EnumerateArray().ToList();

        Assert.Equal(3, items.Count);

        var fmty = items.Single(item => item.GetProperty("ticker").GetString() == "FMTY14");
        Assert.Equal(JsonValueKind.Number, fmty.GetProperty("mercado").GetProperty("precioActual").ValueKind);
        Assert.Equal(JsonValueKind.Null, fmty.GetProperty("fundamentales").GetProperty("navPerCbfi").ValueKind);
        Assert.True(fmty.GetProperty("score").GetProperty("isLimitedData").GetBoolean());
        Assert.False(fmty.GetProperty("score").GetProperty("isExcluded").GetBoolean());

        var terra = items.Single(item => item.GetProperty("ticker").GetString() == "TERRA13");
        Assert.Equal(JsonValueKind.Null, terra.GetProperty("mercado").GetProperty("precioActual").ValueKind);
        Assert.True(terra.GetProperty("score").GetProperty("isExcluded").GetBoolean());
        Assert.Equal(JsonValueKind.Null, terra.GetProperty("score").GetProperty("score").ValueKind);
    }

    [Fact]
    public async Task GetCompare_WithLessThanTwoTickers_Returns400()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/compare?tickers=FUNO11");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCompare_WithMoreThanFourTickers_Returns400()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/compare?tickers=FUNO11,FMTY14,DANHOS13,TERRA13,FIHO12");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCompare_WithInvalidTicker_Returns400_AndIdentifiesTicker()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/compare?tickers=FUNO11,XXXXXX");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);

        Assert.True(doc.RootElement.GetProperty("detail").GetString()?.Contains("XXXXXX", StringComparison.OrdinalIgnoreCase) ?? false);
        Assert.Equal("XXXXXX", doc.RootElement.GetProperty("ticker").GetString());
    }

    [Fact]
    public async Task GetCompare_IsPublic_WithoutJwt()
    {
        await EnsureSeededAsync();

        var response = await _client.GetAsync("/api/v1/compare?tickers=FUNO11,FMTY14");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
