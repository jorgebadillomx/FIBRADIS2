using System.Net;
using System.Net.Http.Json;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class AuthLoginTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Login_ValidUserCredentials_Returns200WithAccessToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("user@test.com", "password123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
    }

    [Fact]
    public async Task Login_ValidAdminOpsCredentials_Returns200WithAccessToken()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("adminops@test.com", "ops123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body.AccessToken));
    }

    [Fact]
    public async Task Login_ValidCredentials_SetsRefreshTokenCookie()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("user@test.com", "password123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("Set-Cookie"));

        var setCookie = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
        Assert.NotNull(setCookie);
        Assert.Contains("refreshToken=", setCookie);
        Assert.Contains("HttpOnly", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("user@test.com", "wrongpassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_NonExistentEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("noexiste@test.com", "password123"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsProblemDetailsWithDomainCode()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("user@test.com", "wrongpassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(body.TryGetProperty("domainCode", out var domainCode));
        Assert.Equal("INVALID_CREDENTIALS", domainCode.GetString());
    }
}
