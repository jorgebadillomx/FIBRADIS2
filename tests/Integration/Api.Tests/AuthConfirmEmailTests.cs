using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class AuthConfirmEmailTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory.EmailService.Clear();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ConfirmEmail_ValidToken_Returns200AndAllowsLogin()
    {
        const string email = "confirmable@fibradis.mx";
        const string password = "Fuerte1!";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            email,
            password,
            null,
            null));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var token = ExtractToken(_factory.EmailService.Emails.Single().ConfirmationUrl);

        var beforeConfirm = DateTimeOffset.UtcNow;
        var confirmResponse = await _client.GetAsync($"/api/v1/auth/confirm-email?token={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var body = await confirmResponse.Content.ReadFromJsonAsync<ConfirmEmailResponse>();
        Assert.NotNull(body);
        Assert.InRange(body!.TrialEndsAt, beforeConfirm.AddDays(13).AddHours(23), beforeConfirm.AddDays(14).AddHours(1));

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.SqlServer.AppDbContext>();
        var stored = await db.Users.SingleAsync();
        Assert.True(stored.IsActive);
        Assert.NotNull(stored.EmailConfirmedAt);
        Assert.NotNull(stored.TrialEndsAt);
    }

    [Fact]
    public async Task ConfirmEmail_ExpiredToken_Returns400TokenExpired()
    {
        var expiredToken = CreateToken(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds());

        var response = await _client.GetAsync($"/api/v1/auth/confirm-email?token={Uri.EscapeDataString(expiredToken)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("token_expired", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ConfirmEmail_AlreadyUsedToken_Returns400TokenAlreadyUsed()
    {
        const string email = "already-used@fibradis.mx";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            email,
            "Fuerte1!",
            null,
            null));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var token = ExtractToken(_factory.EmailService.Emails.Single().ConfirmationUrl);

        var firstConfirm = await _client.GetAsync($"/api/v1/auth/confirm-email?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.OK, firstConfirm.StatusCode);

        var secondConfirm = await _client.GetAsync($"/api/v1/auth/confirm-email?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.BadRequest, secondConfirm.StatusCode);

        var body = await secondConfirm.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("token_already_used", body.GetProperty("code").GetString());
    }

    private static string ExtractToken(string confirmationUrl)
    {
        var uri = new Uri(confirmationUrl);
        var query = uri.Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && parts[0] == "token")
                return Uri.UnescapeDataString(parts[1]);
        }

        throw new InvalidOperationException("No se encontró token en confirmationUrl.");
    }

    private static string CreateToken(Guid userId, long expiryUnix)
    {
        const string secret = "test-secret-key-must-be-at-least-32-chars-long!!!";
        var payload = $"{userId}|{expiryUnix}";

        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes("fibradis-email-confirmation:" + secret));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
