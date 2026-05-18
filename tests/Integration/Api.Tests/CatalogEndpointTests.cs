using System.Net;
using System.Text.Json;

namespace Api.Tests;

public class CatalogEndpointTests(ApiWebFactory factory)
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
                await factory.SeedCatalogAsync();
                _seeded = true;
            }
        }
        finally { _lock.Release(); }
    }

    [Fact]
    public async Task GetFibras_ReturnsOk_WithPagedResult()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("items", out var items));
        Assert.True(root.TryGetProperty("page", out _));
        Assert.True(root.TryGetProperty("pageSize", out _));
        Assert.True(root.TryGetProperty("total", out _));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    [Fact]
    public async Task GetFibras_ExcludesInactiveFibras()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items").EnumerateArray();

        Assert.DoesNotContain(items, item =>
            item.GetProperty("ticker").GetString() == "INACTIVA1");
    }

    [Fact]
    public async Task GetFibras_EachItemHasRequiredFields()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var firstItem = doc.RootElement.GetProperty("items").EnumerateArray().First();

        Assert.True(firstItem.TryGetProperty("ticker", out _));
        Assert.True(firstItem.TryGetProperty("fullName", out _));
        Assert.True(firstItem.TryGetProperty("shortName", out _));
        Assert.True(firstItem.TryGetProperty("sector", out _));
        Assert.True(firstItem.TryGetProperty("market", out _));
        Assert.True(firstItem.TryGetProperty("currency", out _));
        Assert.True(firstItem.TryGetProperty("state", out _));
    }

    [Fact]
    public async Task GetFibraByTicker_ReturnsOk_WithFullDetail()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras/FUNO11");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.Equal("FUNO11", root.GetProperty("ticker").GetString());
        Assert.True(root.TryGetProperty("nameVariants", out var variants));
        Assert.Equal(JsonValueKind.Array, variants.ValueKind);
        Assert.True(root.TryGetProperty("investorUrl", out _));
        Assert.True(root.TryGetProperty("reportsUrl", out _));
    }

    [Fact]
    public async Task GetFibraByTicker_InactiveFibra_IsAccessible()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras/INACTIVA1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetFibraByTicker_NonExistentTicker_Returns404WithProblemDetails()
    {
        await EnsureSeededAsync();
        var response = await _client.GetAsync("/api/v1/fibras/FAKE99");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.TryGetProperty("domainCode", out _));
        Assert.True(doc.RootElement.TryGetProperty("correlationId", out _));
    }
}
