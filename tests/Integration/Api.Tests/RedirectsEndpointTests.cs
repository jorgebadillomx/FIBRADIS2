using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Persistence.SqlServer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;
using SharedApiContracts.Seo;

namespace Api.Tests;

public class RedirectsEndpointTests : IAsyncLifetime
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

        _anonClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
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
    public async Task OpsRedirectEndpoints_RequireAdminOps()
    {
        var anonResponse = await _anonClient.GetAsync("/api/v1/ops/seo/redirects");
        var userResponse = await _userClient.GetAsync("/api/v1/ops/seo/redirects");

        Assert.Equal(HttpStatusCode.Unauthorized, anonResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, userResponse.StatusCode);
    }

    [Fact]
    public async Task OpsRedirectCrud_AndPublicRedirect_WorkEndToEnd()
    {
        await ResetRedirectsAsync();

        var createResponse = await _adminClient.PostAsJsonAsync(
            "/api/v1/ops/seo/redirects",
            new UpsertUrlRedirectRequest(
                "/Blog/",
                "/Noticias/",
                301,
                true,
                "Legacy blog path"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<UrlRedirectDto>();
        Assert.NotNull(created);
        Assert.Equal("/blog", created!.FromPath);
        Assert.Equal("/noticias", created.ToPath);
        Assert.Equal(301, created.StatusCode);
        Assert.True(created.IsActive);

        var redirectResponse = await _anonClient.GetAsync("/blog?utm_source=google");
        Assert.Equal(HttpStatusCode.MovedPermanently, redirectResponse.StatusCode);
        Assert.Equal("/noticias?utm_source=google", redirectResponse.Headers.Location?.ToString());

        var updateResponse = await _adminClient.PutAsJsonAsync(
            $"/api/v1/ops/seo/redirects/{created.Id}",
            new UpsertUrlRedirectRequest(
                "/Blog/",
                "/Privacidad/",
                302,
                true,
                "Updated redirect"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<UrlRedirectDto>();
        Assert.NotNull(updated);
        Assert.Equal(302, updated!.StatusCode);
        Assert.Equal("/privacidad", updated.ToPath);

        var deactivateResponse = await _adminClient.PostAsync($"/api/v1/ops/seo/redirects/{created.Id}/deactivate", content: null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        // Redirect desactivado → /blog ya no se redirige. Como /blog no es una ruta SPA conocida,
        // el fallback responde 404 (soft-404 de H1: evita index bloat), sirviendo igual el shell.
        var passthroughResponse = await _anonClient.GetAsync("/blog");
        var passthroughBody = await passthroughResponse.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.NotFound, passthroughResponse.StatusCode);
        Assert.Contains("<html", passthroughBody);

        var activateResponse = await _adminClient.PostAsync($"/api/v1/ops/seo/redirects/{created.Id}/activate", content: null);
        Assert.Equal(HttpStatusCode.OK, activateResponse.StatusCode);
    }

    [Fact]
    public async Task OpsRedirects_CreateDuplicateFromPath_ReturnsConflict()
    {
        await ResetRedirectsAsync();

        var firstResponse = await _adminClient.PostAsJsonAsync(
            "/api/v1/ops/seo/redirects",
            new UpsertUrlRedirectRequest("/blog", "/noticias", 301, true, null));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await _adminClient.PostAsJsonAsync(
            "/api/v1/ops/seo/redirects",
            new UpsertUrlRedirectRequest("/BLOG/", "/privacidad", 302, true, null));

        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task OpsRedirects_BlocksReverseLoopPair()
    {
        await ResetRedirectsAsync();

        var firstResponse = await _adminClient.PostAsJsonAsync(
            "/api/v1/ops/seo/redirects",
            new UpsertUrlRedirectRequest("/legacy-a", "/legacy-b", 301, true, null));

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var loopResponse = await _adminClient.PostAsJsonAsync(
            "/api/v1/ops/seo/redirects",
            new UpsertUrlRedirectRequest("/legacy-b", "/legacy-a", 301, true, null));

        Assert.Equal(HttpStatusCode.BadRequest, loopResponse.StatusCode);
    }

    [Fact]
    public async Task PublicRedirect_EmitsConfigured302_WithQueryString()
    {
        await ResetRedirectsAsync();

        var createResponse = await _adminClient.PostAsJsonAsync(
            "/api/v1/ops/seo/redirects",
            new UpsertUrlRedirectRequest("/promo-vieja", "/promo", 302, true, null));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var redirectResponse = await _anonClient.GetAsync("/promo-vieja?ref=mail");

        Assert.Equal(HttpStatusCode.Found, redirectResponse.StatusCode); // 302
        Assert.Equal("/promo?ref=mail", redirectResponse.Headers.Location?.ToString());
    }

    [Theory]
    [InlineData("https://evil.com")]
    [InlineData("//evil.com")]
    [InlineData("/\\evil.com")]
    public async Task OpsRedirects_RejectsExternalToPath_WithValidationError(string toPath)
    {
        await ResetRedirectsAsync();

        var response = await _adminClient.PostAsJsonAsync(
            "/api/v1/ops/seo/redirects",
            new UpsertUrlRedirectRequest("/landing", toPath, 301, true, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var loginResponse = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginBody!.AccessToken;
    }

    private async Task ResetRedirectsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();

        db.UrlRedirects.RemoveRange(db.UrlRedirects.ToList());
        await db.SaveChangesAsync();
    }
}
