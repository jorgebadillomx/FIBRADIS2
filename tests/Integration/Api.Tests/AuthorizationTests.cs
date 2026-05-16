using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class AuthorizationTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _client = null!;
    private string _userToken = string.Empty;
    private string _adminOpsToken = string.Empty;

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();
        _client = _factory.CreateClient();

        // Obtener token de Usuario
        var userLogin = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("user@test.com", "password123"));
        var userBody = await userLogin.Content.ReadFromJsonAsync<LoginResponse>();
        _userToken = userBody!.AccessToken;

        // Obtener token de AdminOps
        var adminLogin = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("adminops@test.com", "admin456"));
        var adminBody = await adminLogin.Content.ReadFromJsonAsync<LoginResponse>();
        _adminOpsToken = adminBody!.AccessToken;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    // CA-6a: Ruta pública sin token → 200
    [Fact]
    public async Task PublicRoute_WithoutToken_Returns200()
    {
        var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // CA-6b: Ruta privada sin token → 401
    [Fact]
    public async Task PrivateRoute_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // CA-3: Ruta privada con JWT de Usuario → 200
    [Fact]
    public async Task PrivateRoute_WithUserToken_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _userToken);

        var response = await _client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // CA-4: Ruta Ops con JWT de Usuario → 403
    [Fact]
    public async Task OpsRoute_WithUserToken_Returns403()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _userToken);

        var response = await _client.GetAsync("/api/v1/ops/ping");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // CA-5: Ruta Ops con JWT de AdminOps → 200
    [Fact]
    public async Task OpsRoute_WithAdminOpsToken_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _adminOpsToken);

        var response = await _client.GetAsync("/api/v1/ops/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // AdminOps también puede acceder a rutas privadas
    [Fact]
    public async Task PrivateRoute_WithAdminOpsToken_Returns200()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _adminOpsToken);

        var response = await _client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Token inválido → 401
    [Fact]
    public async Task PrivateRoute_WithInvalidToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "este.token.esinvalido");

        var response = await _client.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
