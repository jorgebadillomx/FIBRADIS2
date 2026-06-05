using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure.Persistence.SqlServer;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;

namespace Api.Tests.Ops;

public class OpsUserEndpointTests(ApiWebFactory factory) : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory = factory;
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

    public Task DisposeAsync() => Task.CompletedTask;

    // ── GET /api/v1/ops/users ────────────────────────────────────────────────

    [Fact]
    public async Task GetUsers_WithAdminToken_Returns200AndList()
    {
        var response = await _adminClient.GetAsync("/api/v1/ops/users");
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotEmpty(body!);
    }

    [Fact]
    public async Task GetUsers_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/ops/users");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithUserRole_Returns403()
    {
        var response = await _userClient.GetAsync("/api/v1/ops/users");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ── POST /api/v1/ops/users ───────────────────────────────────────────────

    [Fact]
    public async Task CreateUser_ValidMainUser_Returns201WithDecryptedEmail()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/users",
            new CreateUserRequest("nuevo@fibradis.mx", "Fuerte1!", "User", null, null));
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("nuevo@fibradis.mx", body!.Email);
        Assert.Equal("User", body.Role);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task CreateUser_AdminOpsRole_Returns201WithCorrectRole()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/users",
            new CreateUserRequest("ops2@fibradis.mx", "Fuerte1!", "AdminOps", null, null));
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("AdminOps", body!.Role);
    }

    [Fact]
    public async Task CreateUser_WithPaymentFields_Returns201WithPaymentData()
    {
        var fechaPago = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/users",
            new CreateUserRequest("pago@fibradis.mx", "Fuerte1!", "User", 150m, fechaPago));
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(150m, body!.Pago);
        Assert.NotNull(body.FechaPago);
    }

    [Fact]
    public async Task CreateUser_WeakPassword_Returns422()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/users",
            new CreateUserRequest("debil@fibradis.mx", "sinmayuscula1!", "User", null, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_InvalidRole_Returns422()
    {
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/users",
            new CreateUserRequest("x@fibradis.mx", "Fuerte1!", "SuperAdmin", null, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_DuplicateEmail_Returns422()
    {
        await _adminClient.PostAsJsonAsync("/api/v1/ops/users",
            new CreateUserRequest("dup@fibradis.mx", "Fuerte1!", "User", null, null));

        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/users",
            new CreateUserRequest("dup@fibradis.mx", "Fuerte2@", "User", null, null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ── PATCH /api/v1/ops/users/{id}/active ─────────────────────────────────

    [Fact]
    public async Task SetUserActive_Disable_Returns200WithIsActiveFalse()
    {
        var userId = await CreateTestUserAndGetIdAsync("disable@fibradis.mx");

        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{userId}/active",
            new SetUserActiveRequest(false));
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.False(body!.IsActive);
    }

    [Fact]
    public async Task SetUserActive_Enable_Returns200WithIsActiveTrue()
    {
        var userId = await CreateTestUserAndGetIdAsync("enable@fibradis.mx");
        await _adminClient.PatchAsJsonAsync($"/api/v1/ops/users/{userId}/active", new SetUserActiveRequest(false));

        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{userId}/active",
            new SetUserActiveRequest(true));
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body!.IsActive);
    }

    [Fact]
    public async Task SetUserActive_NonExistingUser_Returns404()
    {
        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{Guid.NewGuid()}/active",
            new SetUserActiveRequest(false));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SetUserActive_WithoutToken_Returns401()
    {
        var response = await _anonClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{Guid.NewGuid()}/active",
            new SetUserActiveRequest(false));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── PATCH /api/v1/ops/users/{id}/password ───────────────────────────────

    [Fact]
    public async Task ChangePassword_StrongPassword_Returns204()
    {
        var userId = await CreateTestUserAndGetIdAsync("pwdchange@fibradis.mx");

        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{userId}/password",
            new ChangePasswordRequest("NuevaFuerte2@"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WeakPassword_Returns422()
    {
        var userId = await CreateTestUserAndGetIdAsync("weakpwd@fibradis.mx");

        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{userId}/password",
            new ChangePasswordRequest("sinmayuscula1!"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NonExistingUser_Returns404()
    {
        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{Guid.NewGuid()}/password",
            new ChangePasswordRequest("Fuerte1!"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_NewPasswordWorksOnLogin()
    {
        var email = "newpwd@fibradis.mx";
        var userId = await CreateTestUserAndGetIdAsync(email);

        await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{userId}/password",
            new ChangePasswordRequest("NuevaClave3#"));

        var loginResponse = await _anonClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "NuevaClave3#"));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    // ── PATCH /api/v1/ops/users/{id}/payment ────────────────────────────────

    [Fact]
    public async Task UpdatePayment_ValidValues_Returns200WithUpdatedData()
    {
        var userId = await CreateTestUserAndGetIdAsync("payment@fibradis.mx");

        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{userId}/payment",
            new UpdatePaymentRequest(300m, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc)));
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(300m, body!.Pago);
        Assert.NotNull(body.FechaPago);
    }

    [Fact]
    public async Task UpdatePayment_NullValues_Returns200WithNullData()
    {
        var userId = await CreateTestUserAndGetIdAsync("nullpay@fibradis.mx");
        await _adminClient.PatchAsJsonAsync($"/api/v1/ops/users/{userId}/payment",
            new UpdatePaymentRequest(200m, new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)));

        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{userId}/payment",
            new UpdatePaymentRequest(null, null));
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(body!.Pago);
        Assert.Null(body.FechaPago);
    }

    [Fact]
    public async Task UpdatePayment_NonExistingUser_Returns404()
    {
        var response = await _adminClient.PatchAsJsonAsync(
            $"/api/v1/ops/users/{Guid.NewGuid()}/payment",
            new UpdatePaymentRequest(100m, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Login con cuenta deshabilitada (AC-9) ─────────────────────────────

    [Fact]
    public async Task Login_DisabledAccount_Returns401WithAccountDisabledCode()
    {
        var email = "disabled@fibradis.mx";
        var userId = await CreateTestUserAndGetIdAsync(email);
        await _adminClient.PatchAsJsonAsync($"/api/v1/ops/users/{userId}/active", new SetUserActiveRequest(false));

        var response = await _anonClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "Fuerte1!"));
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(body.TryGetProperty("domainCode", out var code));
        Assert.Equal("ACCOUNT_DISABLED", code.GetString());
    }

    [Fact]
    public async Task Login_DisabledAccountWrongPassword_Returns401WithInvalidCredentials()
    {
        var email = "disabledwrong@fibradis.mx";
        var userId = await CreateTestUserAndGetIdAsync(email);
        await _adminClient.PatchAsJsonAsync($"/api/v1/ops/users/{userId}/active", new SetUserActiveRequest(false));

        var response = await _anonClient.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, "WrongPass9!"));
        var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.True(body.TryGetProperty("domainCode", out var code));
        // Cuenta deshabilitada detectada antes que password — devuelve ACCOUNT_DISABLED per spec
        Assert.Equal("ACCOUNT_DISABLED", code.GetString());
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login", new LoginRequest(email, password));
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }

    private async Task<Guid> CreateTestUserAndGetIdAsync(string email)
    {
        var response = await _adminClient.PostAsJsonAsync("/api/v1/ops/users",
            new CreateUserRequest(email, "Fuerte1!", "User", null, null));
        var body = await response.Content.ReadFromJsonAsync<UserSummaryDto>();
        return body!.Id;
    }
}
