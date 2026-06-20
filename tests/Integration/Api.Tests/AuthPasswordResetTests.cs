using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.Auth;
using Domain.Auth;
using Infrastructure.Persistence.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests;

public class AuthPasswordResetTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory.EmailService.Clear();
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ForgotPassword_ConfirmedUser_Returns200AndQueuesResetEmail()
    {
        const string email = "resetme@fibradis.mx";
        const string password = "Fuerte1!x";
        await SeedUserAsync(email, password, confirmed: true);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = email });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Si ese email está registrado, recibirás un enlace.", body.GetProperty("message").GetString());

        Assert.Single(_factory.EmailService.PasswordResetEmails);
        var resetEmail = _factory.EmailService.PasswordResetEmails[0];
        Assert.Equal(email, resetEmail.ToEmail);
        Assert.Contains("/api/v1/auth/reset-password-redirect?token=", resetEmail.ResetUrl);
    }

    [Fact]
    public async Task ForgotPassword_UnconfirmedOrMissingUser_Returns200WithoutSendingEmail()
    {
        await SeedUserAsync("unconfirmed@fibradis.mx", "Fuerte1!x", confirmed: false);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = "unconfirmed@fibradis.mx" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(_factory.EmailService.PasswordResetEmails);

        var missingResponse = await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { Email = "missing@fibradis.mx" });
        Assert.Equal(HttpStatusCode.OK, missingResponse.StatusCode);
        Assert.Empty(_factory.EmailService.PasswordResetEmails);
    }

    [Fact]
    public async Task ResetPasswordRedirect_ValidToken_RedirectsToTokenUrl()
    {
        var (userId, passwordHash) = await SeedConfirmedUserAsync("redirect-valid@fibradis.mx", "Fuerte1!x");
        var tokenService = GetTokenService();
        var token = tokenService.GenerateToken(userId, passwordHash);

        var response = await _client.GetAsync($"/api/v1/auth/reset-password-redirect?token={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/nueva-contrasena?token=", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ResetPasswordRedirect_ExpiredToken_RedirectsToExpiredState()
    {
        var (userId, passwordHash) = await SeedConfirmedUserAsync("redirect-expired@fibradis.mx", "Fuerte1!x");
        var token = CreateToken(userId, passwordHash, DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds());

        var response = await _client.GetAsync($"/api/v1/auth/reset-password-redirect?token={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/nueva-contrasena?status=expired", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ResetPasswordRedirect_InvalidOrMissingToken_RedirectsToInvalidState()
    {
        var invalidResponse = await _client.GetAsync("/api/v1/auth/reset-password-redirect?token=invalid.token");
        Assert.Equal(HttpStatusCode.Found, invalidResponse.StatusCode);
        Assert.Contains("/nueva-contrasena?status=invalid", invalidResponse.Headers.Location?.ToString());

        var missingResponse = await _client.GetAsync("/api/v1/auth/reset-password-redirect");
        Assert.Equal(HttpStatusCode.Found, missingResponse.StatusCode);
        Assert.Contains("/nueva-contrasena?status=invalid", missingResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ResetPassword_ValidToken_UpdatesPasswordAndInvalidatesToken()
    {
        const string email = "reset-flow@fibradis.mx";
        var (userId, passwordHash) = await SeedConfirmedUserAsync(email, "Fuerte1!x");
        var tokenService = GetTokenService();
        var token = tokenService.GenerateToken(userId, passwordHash);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            Token = token,
            NewPassword = "Nueva1!x",
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Contraseña actualizada.", body.GetProperty("message").GetString());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stored = await db.Users.SingleAsync(u => u.Id == userId);
            Assert.True(BCrypt.Net.BCrypt.Verify("Nueva1!x", stored.PasswordHash));
        }

        var repeatResponse = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            Token = token,
            NewPassword = "Otra1!xy",
        });

        Assert.Equal(HttpStatusCode.BadRequest, repeatResponse.StatusCode);
        var repeatBody = await repeatResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("token_invalid", repeatBody.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ResetPassword_WeakPassword_ReturnsValidationDetail()
    {
        var (userId, passwordHash) = await SeedConfirmedUserAsync("reset-weak@fibradis.mx", "Fuerte1!x");
        var tokenService = GetTokenService();
        var token = tokenService.GenerateToken(userId, passwordHash);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            Token = token,
            NewPassword = "sinmayuscula1!",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("mayúscula", body.GetProperty("detail").GetString() ?? string.Empty);
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsTokenInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new
        {
            Token = "invalid.token",
            NewPassword = "Nueva1!x",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("token_invalid", body.GetProperty("code").GetString());
    }

    private async Task<(Guid UserId, string PasswordHash)> SeedConfirmedUserAsync(string email, string password)
    {
        var user = await SeedUserAsync(email, password, confirmed: true);
        return (user.Id, user.PasswordHash);
    }

    private async Task<User> SeedUserAsync(string email, string password, bool confirmed)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var encryptor = scope.ServiceProvider.GetRequiredService<IEmailEncryptor>();

        await db.Database.EnsureCreatedAsync();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = encryptor.Encrypt(email.Trim().ToLowerInvariant()),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.User,
            IsActive = confirmed,
            CreatedAt = DateTime.UtcNow,
            EmailConfirmedAt = confirmed ? DateTime.UtcNow : null,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private IPasswordResetTokenService GetTokenService()
    {
        using var scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IPasswordResetTokenService>();
    }

    private string CreateToken(Guid userId, string passwordHash, long expiryUnix)
    {
        using var scope = _factory.Services.CreateScope();
        var secret = scope.ServiceProvider.GetRequiredService<IConfiguration>()["Jwt:Secret"]!;
        var hashPrefix = passwordHash[..Math.Min(12, passwordHash.Length)];
        var payload = $"{userId}|{expiryUnix}";
        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes("fibradis-password-reset:" + secret + hashPrefix));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
