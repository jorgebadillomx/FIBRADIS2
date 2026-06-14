using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Seo;

namespace Api.Tests;

public class SeoFaqEndpointTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _adminClient = null!;
    private HttpClient _userClient = null!;
    private HttpClient _anonClient = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();

        _adminClient = _factory.CreateClient();
        _adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("adminops@test.com", "ops123"));

        _userClient = _factory.CreateClient();
        _userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("user@test.com", "password123"));

        _anonClient = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _adminClient.Dispose();
        _userClient.Dispose();
        _anonClient.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetFaq_PublicEndpoint_ReturnsOnlyActiveItems()
    {
        await ResetFaqAsync();
        await SeedFaqAsync();

        var response = await _anonClient.GetAsync("/api/v1/faq?pageType=StaticPage&entityKey=%2Ffundamentales");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<FaqItemDto>>();
        Assert.NotNull(body);
        Assert.Equal(2, body!.Count);
        Assert.All(body, item => Assert.True(item.IsActive));
        Assert.Equal("¿Qué es NAV por CBFI?", body[0].Question);
    }

    [Fact]
    public async Task OpsFaqEndpoints_RequireAdminOps()
    {
        var anonResponse = await _anonClient.GetAsync("/api/v1/ops/seo/faq?pageType=StaticPage&entityKey=%2Ffundamentales");
        var userResponse = await _userClient.GetAsync("/api/v1/ops/seo/faq?pageType=StaticPage&entityKey=%2Ffundamentales");

        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
    }

    [Fact]
    public async Task OpsFaqCrud_CreateUpdateDeactivate_WorksEndToEnd()
    {
        await ResetFaqAsync();

        var createResponse = await _adminClient.PostAsJsonAsync(
            "/api/v1/ops/seo/faq",
            new UpsertFaqItemRequest(
                "StaticPage",
                "/fundamentales/",
                "¿Qué es la distribución trimestral?",
                "Pago en efectivo por CBFI cada trimestre.",
                7,
                true));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<FaqItemDto>();
        Assert.NotNull(created);
        Assert.Equal("/fundamentales", created!.EntityKey);
        Assert.True(created.IsActive);

        var updateResponse = await _adminClient.PutAsJsonAsync(
            $"/api/v1/ops/seo/faq/{created.Id}",
            new UpsertFaqItemRequest(
                "StaticPage",
                "/fundamentales",
                "¿Qué es la distribución trimestral?",
                "Distribución = Resultado Fiscal Distribuido + Reembolso de Capital",
                1,
                true));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<FaqItemDto>();
        Assert.NotNull(updated);
        Assert.Equal(1, updated!.Order);
        Assert.Equal("Distribución = Resultado Fiscal Distribuido + Reembolso de Capital", updated.Answer);

        var deleteResponse = await _adminClient.DeleteAsync($"/api/v1/ops/seo/faq/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var publicResponse = await _anonClient.GetAsync("/api/v1/faq?pageType=StaticPage&entityKey=%2Ffundamentales");
        var publicBody = await publicResponse.Content.ReadFromJsonAsync<List<FaqItemDto>>();
        Assert.DoesNotContain(publicBody!, item => item.Id == created.Id);
    }

    [Fact]
    public async Task OpsFaqSeed_CreatesEditorialAndFundamentalsItems()
    {
        await ResetFaqAsync();

        var response = await _adminClient.PostAsync("/api/v1/ops/seo/faq/seed", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<FaqSeedResultDto>();
        Assert.NotNull(body);
        Assert.True(body!.CreatedCount >= 11);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeded = await db.FaqItems.Where(item => item.IsActive).ToListAsync();

        Assert.Contains(seeded, item => item.EntityKey == "/conoce-las-fibras" && item.Question == "¿Qué son las FIBRAs?");
        Assert.Contains(seeded, item => item.EntityKey == "/fundamentales" && item.Question == "¿Qué es Cap Rate?");
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginBody!.AccessToken;
    }

    private async Task SeedFaqAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        if (await db.FaqItems.AnyAsync())
            return;

        db.FaqItems.AddRange(
            new Domain.Seo.FaqItem
            {
                Id = Guid.NewGuid(),
                PageType = Domain.Seo.SeoPageType.StaticPage,
                EntityKey = "/fundamentales",
                Question = "¿Qué es NAV por CBFI?",
                Answer = "NAV/CBFI = NAV / CBFIs en circulación",
                Order = 1,
                IsActive = true,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "system",
            },
            new Domain.Seo.FaqItem
            {
                Id = Guid.NewGuid(),
                PageType = Domain.Seo.SeoPageType.StaticPage,
                EntityKey = "/fundamentales",
                Question = "¿Qué es Cap Rate?",
                Answer = "Cap Rate = NOI anualizado / Valor de propiedades de inversión",
                Order = 2,
                IsActive = true,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "system",
            },
            new Domain.Seo.FaqItem
            {
                Id = Guid.NewGuid(),
                PageType = Domain.Seo.SeoPageType.StaticPage,
                EntityKey = "/fundamentales",
                Question = "¿Oculta?",
                Answer = "No debe salir.",
                Order = 3,
                IsActive = false,
                UpdatedAt = DateTimeOffset.UtcNow,
                UpdatedBy = "system",
            });

        await db.SaveChangesAsync();
    }

    private async Task ResetFaqAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        db.FaqItems.RemoveRange(db.FaqItems);
        await db.SaveChangesAsync();
    }
}
