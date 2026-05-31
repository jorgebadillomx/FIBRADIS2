using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Domain.Catalog;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Catalog;

namespace Api.Tests.Ops;

public class FibraDescriptionTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory = factory;
    private HttpClient _adminClient = null!;
    private HttpClient _anonClient = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        await _factory.SeedCatalogAsync();
        await SeedDescriptionFibrasAsync();

        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("adminops@test.com", "ops123"));

        _anonClient = _factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetFibras_HasDescriptionTrue_WhenFibraHasDescription()
    {
        var response = await _anonClient.GetAsync("/api/v1/fibras?pageSize=100");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();

        var withDesc = items.FirstOrDefault(i => i.GetProperty("ticker").GetString() == "FDESC99");
        Assert.NotEqual(default, withDesc);
        Assert.True(withDesc.GetProperty("hasDescription").GetBoolean());
    }

    [Fact]
    public async Task GetFibras_HasDescriptionFalse_WhenFibraHasNullDescription()
    {
        var response = await _anonClient.GetAsync("/api/v1/fibras?pageSize=100");
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var items = doc.RootElement.GetProperty("items").EnumerateArray().ToList();

        var noDesc = items.FirstOrDefault(i => i.GetProperty("ticker").GetString() == "FNODESC99");
        Assert.NotEqual(default, noDesc);
        Assert.False(noDesc.GetProperty("hasDescription").GetBoolean());
    }

    [Fact]
    public async Task PutCatalog_UpdatesDescription_VerifiedViaPublicDetail()
    {
        var updatePayload = new UpdateFibraRequest(
            YahooTicker: "FDESC99.MX",
            FullName: "Fibra Descripción Test",
            ShortName: "FDESC",
            Sector: "Industrial",
            Market: "BMV",
            Currency: "MXN",
            SiteUrl: null,
            InvestorUrl: null,
            ReportsUrl: null,
            NameVariants: [],
            Description: "# Texto actualizado\n\nDescripción editada en el test.");

        var putResponse = await _adminClient.PutAsJsonAsync("/api/v1/ops/catalog/FDESC99", updatePayload);
        Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

        var getResponse = await _anonClient.GetAsync("/api/v1/fibras/FDESC99");
        var detail = await getResponse.Content.ReadFromJsonAsync<FibraDetail>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(detail);
        Assert.Equal("# Texto actualizado\n\nDescripción editada en el test.", detail!.Description);
    }

    private async Task SeedDescriptionFibrasAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!await db.Fibras.AnyAsync(f => f.Ticker == "FDESC99"))
        {
            db.Fibras.Add(new Fibra
            {
                Id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000001"),
                Ticker = "FDESC99",
                YahooTicker = "FDESC99.MX",
                FullName = "Fibra Descripción Test",
                ShortName = "FDESC",
                Sector = "Industrial",
                Market = "BMV",
                Currency = "MXN",
                State = FibraState.Active,
                NameVariants = [],
                CreatedAt = DateTimeOffset.UtcNow,
                Description = "# Descripción inicial\n\nTexto de prueba.",
            });
        }

        if (!await db.Fibras.AnyAsync(f => f.Ticker == "FNODESC99"))
        {
            db.Fibras.Add(new Fibra
            {
                Id = Guid.Parse("eeeeeeee-0000-0000-0000-000000000002"),
                Ticker = "FNODESC99",
                YahooTicker = "FNODESC99.MX",
                FullName = "Fibra Sin Descripción Test",
                ShortName = "FNODESC",
                Sector = "Industrial",
                Market = "BMV",
                Currency = "MXN",
                State = FibraState.Active,
                NameVariants = [],
                CreatedAt = DateTimeOffset.UtcNow,
                Description = null,
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginBody!.AccessToken;
    }
}
