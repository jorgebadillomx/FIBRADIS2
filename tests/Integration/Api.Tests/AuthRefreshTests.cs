using System.Net;
using System.Net.Http.Json;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class AuthRefreshTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
        });
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Refresh_WithValidRefreshTokenCookie_Returns200WithNewAccessToken()
    {
        // Primero hacemos login para obtener la cookie
        await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("user@test.com", "password123"));

        // Luego hacemos refresh — el HttpClient gestiona cookies automáticamente
        var refreshResponse = await _client.PostAsync("/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var body = await refreshResponse.Content.ReadFromJsonAsync<RefreshResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
    }

    [Fact]
    public async Task Refresh_EmitesNewCookieOnSuccess()
    {
        await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("user@test.com", "password123"));

        var refreshResponse = await _client.PostAsync("/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.True(refreshResponse.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Refresh_WithoutCookie_Returns401()
    {
        // Cliente sin cookies
        var clientWithoutCookies = _factory.CreateClient();
        var response = await clientWithoutCookies.PostAsync("/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        clientWithoutCookies.Dispose();
    }

    [Fact]
    public async Task Refresh_ReusingOldTokenAfterRotation_Returns401()
    {
        // Cliente con control manual de cookies para poder reenviar el token viejo
        using var manualClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });

        // Login — capturar cookie de refresh original
        var loginResponse = await manualClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("user@test.com", "password123"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var setCookie = loginResponse.Headers
            .GetValues("Set-Cookie")
            .First(c => c.StartsWith("refreshToken="));
        var oldCookieNameValue = setCookie.Split(';')[0]; // "refreshToken=<valor>"

        // Primer refresh con el token original — debe rotar el token
        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        req1.Headers.Add("Cookie", oldCookieNameValue);
        var firstRefresh = await manualClient.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        // Reutilizar el token ya revocado — debe retornar 401
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        req2.Headers.Add("Cookie", oldCookieNameValue);
        var secondRefresh = await manualClient.SendAsync(req2);
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
    }
}
