using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class AccountEndpointTests : IClassFixture<ApiWebFactory>, IAsyncLifetime
{
    private readonly ApiWebFactory _factory;
    private HttpClient _userClient = null!;
    private HttpClient _anonClient = null!;

    public AccountEndpointTests(ApiWebFactory factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.SeedUsersAsync();

        _userClient = _factory.CreateClient();
        _userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", await LoginAndGetTokenAsync("user@test.com", "password123"));

        _anonClient = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _userClient.Dispose();
        _anonClient.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        var response = await _anonClient.GetAsync("/api/v1/account/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WithUserToken_Returns200WithEmailAndRole()
    {
        var response = await _userClient.GetAsync("/api/v1/account/me");
        var body = await response.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("user@test.com", body!.Email);
        Assert.Equal("User", body.Role);
        Assert.Equal("Usuario", body.Apodo);
    }

    [Fact]
    public async Task UpdateApodo_WithoutToken_Returns401()
    {
        var response = await _anonClient.PatchAsJsonAsync(
            "/api/v1/account/me",
            new UpdateApodoRequest("Mi apodo"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateApodo_Valid_Returns204AndGetProfileReturnsApodo()
    {
        var response = await _userClient.PatchAsJsonAsync(
            "/api/v1/account/me",
            new UpdateApodoRequest("Mi apodo"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var profileResponse = await _userClient.GetAsync("/api/v1/account/me");
        var profile = await profileResponse.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
        Assert.Equal("Mi apodo", profile!.Apodo);
    }

    [Fact]
    public async Task UpdateApodo_TooLong_Returns400()
    {
        var response = await _userClient.PatchAsJsonAsync(
            "/api/v1/account/me",
            new UpdateApodoRequest(new string('a', 51)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutToken_Returns401()
    {
        var response = await _anonClient.PatchAsJsonAsync(
            "/api/v1/account/password",
            new ChangeOwnPasswordRequest("password123", "NuevaClave9!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ValidCurrentAndStrongNew_Returns204()
    {
        var response = await _userClient.PatchAsJsonAsync(
            "/api/v1/account/password",
            new ChangeOwnPasswordRequest("password123", "NuevaClave9!"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var loginResponse = await _anonClient.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest("user@test.com", "NuevaClave9!"));

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Returns401()
    {
        var response = await _userClient.PatchAsJsonAsync(
            "/api/v1/account/password",
            new ChangeOwnPasswordRequest("incorrecta123", "NuevaClave9!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WeakNew_Returns400()
    {
        var response = await _userClient.PatchAsJsonAsync(
            "/api/v1/account/password",
            new ChangeOwnPasswordRequest("password123", "sinmayuscula1!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetProfile_WhenUserHasMonthlySubscription_ReturnsSubscriptionTypeAndEndsAt()
    {
        var endsAt = DateTime.UtcNow.AddDays(30);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.SqlServer.AppDbContext>();
            var user = await db.Users.FindAsync(Guid.Parse("11111111-0000-0000-0000-000000000001"));
            user!.SubscriptionType = Domain.Auth.SubscriptionType.Monthly;
            user.SubscriptionStartedAt = DateTime.UtcNow.AddDays(-5);
            user.SubscriptionEndsAt = endsAt;
            await db.SaveChangesAsync();
        }

        var response = await _userClient.GetAsync("/api/v1/account/me");
        var body = await response.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Monthly", body!.SubscriptionType);
        Assert.NotNull(body.SubscriptionEndsAt);
    }

    private async Task<string> LoginAndGetTokenAsync(string email, string password)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.AccessToken;
    }
}
