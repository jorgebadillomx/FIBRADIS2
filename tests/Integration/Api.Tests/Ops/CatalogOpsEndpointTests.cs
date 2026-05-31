using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Catalog;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Catalog;

namespace Api.Tests.Ops;

public class CatalogOpsEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory = factory;
    private HttpClient _adminClient = null!;
    private HttpClient _userClient = null!;
    private HttpClient _anonClient = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        await _factory.SeedCatalogAsync();

        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("adminops@test.com", "ops123"));

        _userClient = _factory.CreateClient();
        _userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("user@test.com", "password123"));

        _anonClient = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        if (_adminClient is not null)
        {
            try { await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog/DANHOS13/activate", (object?)null); }
            catch { }
        }
    }

    [Fact]
    public async Task PostCatalog_WithCompletePayload_Returns201Created()
    {
        var payload = CreateRequest("NUEVA25");

        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog", payload);
        var body = await response.Content.ReadFromJsonAsync<FibraDetail>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("NUEVA25", body!.Ticker);
        Assert.Equal("NUEVA25.MX", body.YahooTicker);
        Assert.Equal("Active", body.State);
    }

    [Fact]
    public async Task PostCatalog_WithDuplicateTicker_Returns409Conflict()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog", CreateRequest("FUNO11"));
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("TICKER_ALREADY_EXISTS", doc.RootElement.GetProperty("domainCode").GetString());
    }

    [Fact]
    public async Task PostCatalog_WithMissingRequiredFields_Returns400BadRequest()
    {
        var payload = new CreateFibraRequest(
            Ticker: "",
            YahooTicker: "",
            FullName: "",
            ShortName: "",
            Sector: "",
            Market: "",
            Currency: "INVALIDA",
            SiteUrl: null,
            InvestorUrl: null,
            ReportsUrl: null,
            NameVariants: null,
            Description: null);

        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog", payload);
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(doc.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.EnumerateObject().Any());
    }

    [Fact]
    public async Task PostCatalog_WithoutToken_Returns401()
    {
        var response = await _anonClient.PostAsJsonAsync("/api/v1/ops/catalog", CreateRequest("ANON25"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostCatalog_WithUserRole_Returns403()
    {
        var response = await _userClient.PostAsJsonAsync("/api/v1/ops/catalog", CreateRequest("USER25"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PutCatalog_UpdatesNameAndVariants_Returns200Ok()
    {
        var payload = new UpdateFibraRequest(
            YahooTicker: "FUNO11.MX",
            FullName: "Fibra Uno Editada",
            ShortName: "FUNO",
            Sector: "Diversificado",
            Market: "BMV",
            Currency: "MXN",
            SiteUrl: "https://fibra.uno",
            InvestorUrl: "https://fibra.uno/ri",
            ReportsUrl: "https://fibra.uno/reportes",
            NameVariants: ["Fibra Uno", "FUNO", "FUNO11"],
            Description: null);

        var response = await _adminClient.PutAsJsonAsync("/api/v1/ops/catalog/FUNO11", payload);
        var body = await response.Content.ReadFromJsonAsync<FibraDetail>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Fibra Uno Editada", body!.FullName);
        Assert.Contains("FUNO11", body.NameVariants);
    }

    [Fact]
    public async Task PutCatalog_WithUnknownTicker_Returns404()
    {
        var response = await _adminClient.PutAsJsonAsync("/api/v1/ops/catalog/FAKE999", new UpdateFibraRequest(
            YahooTicker: "FAKE999.MX",
            FullName: "Fake",
            ShortName: "Fake",
            Sector: "Industrial",
            Market: "BMV",
            Currency: "MXN",
            SiteUrl: null,
            InvestorUrl: null,
            ReportsUrl: null,
            NameVariants: [],
            Description: null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostDeactivate_Returns200AndInactive()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog/DANHOS13/deactivate", (object?)null);
        var body = await response.Content.ReadFromJsonAsync<FibraDetail>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Inactive", body!.State);
    }

    [Fact]
    public async Task PostDeactivate_SecondCallIsIdempotent()
    {
        await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog/DANHOS13/deactivate", (object?)null);
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog/DANHOS13/deactivate", (object?)null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PostActivate_Returns200AndActive()
    {
        await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog/DANHOS13/deactivate", (object?)null);
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog/DANHOS13/activate", (object?)null);
        var body = await response.Content.ReadFromJsonAsync<FibraDetail>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Active", body!.State);
    }

    [Fact]
    public async Task GetOpsCatalog_WithAdminToken_Returns200AndIncludesInactive()
    {
        var response = await _adminClient.GetAsync("/api/v1/ops/catalog");
        var body = await response.Content.ReadFromJsonAsync<List<FibraDetail>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(body!, fibra => fibra.Ticker == "INACTIVA1");
    }

    [Fact]
    public async Task GetOpsCatalog_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/ops/catalog");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PublicCatalog_AfterDeactivate_ExcludesDanhos13()
    {
        await _adminClient.PostAsJsonAsync("/api/v1/ops/catalog/DANHOS13/deactivate", (object?)null);

        var response = await _anonClient.GetAsync("/api/v1/fibras");
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(items, item => item.GetProperty("ticker").GetString() == "DANHOS13");
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginBody!.AccessToken;
    }

    private static CreateFibraRequest CreateRequest(string ticker) => new(
        Ticker: ticker,
        YahooTicker: $"{ticker}.MX",
        FullName: $"Fibra {ticker}",
        ShortName: ticker,
        Sector: "Industrial",
        Market: "BMV",
        Currency: "MXN",
        SiteUrl: "https://example.com",
        InvestorUrl: "https://example.com/investors",
        ReportsUrl: "https://example.com/reports",
        NameVariants: [ticker, $"Fibra {ticker}"],
        Description: null);
}
