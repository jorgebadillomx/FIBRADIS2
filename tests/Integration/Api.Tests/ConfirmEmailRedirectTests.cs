using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class ConfirmEmailRedirectTests : IAsyncLifetime
{
    private readonly ApiWebFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory.EmailService.Clear();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
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
    public async Task ConfirmEmailRedirect_ValidToken_Redirects302ToConfirmed()
    {
        const string email = "redirect-confirmed@fibradis.mx";
        const string password = "Fuerte1!";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            email,
            password,
            null,
            null));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var token = ExtractToken(_factory.EmailService.Emails.Single().ConfirmationUrl);
        var beforeConfirm = DateTimeOffset.UtcNow;

        var response = await _client.GetAsync($"/api/v1/auth/confirm-email-redirect?token={Uri.EscapeDataString(token)}");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        var location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/confirmar-email?status=confirmed&t=", location);

        var encodedTrialEndsAt = location![(location.IndexOf("t=", StringComparison.Ordinal) + 2)..];
        var decodedTrialEndsAt = Uri.UnescapeDataString(encodedTrialEndsAt);
        Assert.True(DateTimeOffset.TryParse(decodedTrialEndsAt, out var trialEndsAt));
        Assert.InRange(trialEndsAt, beforeConfirm.AddDays(13).AddHours(23), beforeConfirm.AddDays(14).AddHours(1));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.SqlServer.AppDbContext>();
        var stored = await db.Users.SingleAsync();
        Assert.True(stored.IsActive);
        Assert.NotNull(stored.EmailConfirmedAt);
        Assert.NotNull(stored.TrialEndsAt);
    }

    [Fact]
    public async Task ConfirmEmailRedirect_ExpiredToken_Redirects302ToExpired()
    {
        var expiredToken = CreateToken(
            Guid.Parse("11111111-2222-3333-4444-555555555555"),
            DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds());

        var response = await _client.GetAsync($"/api/v1/auth/confirm-email-redirect?token={Uri.EscapeDataString(expiredToken)}");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Contains("/confirmar-email?status=expired", response.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ConfirmEmailRedirect_AlreadyUsedToken_Redirects302ToAlreadyConfirmed()
    {
        const string email = "redirect-used@fibradis.mx";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            email,
            "Fuerte1!",
            null,
            null));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        var token = ExtractToken(_factory.EmailService.Emails.Single().ConfirmationUrl);

        var firstConfirm = await _client.GetAsync($"/api/v1/auth/confirm-email-redirect?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.Found, firstConfirm.StatusCode);
        Assert.Contains("/confirmar-email?status=confirmed&t=", firstConfirm.Headers.Location?.ToString());

        var secondConfirm = await _client.GetAsync($"/api/v1/auth/confirm-email-redirect?token={Uri.EscapeDataString(token)}");
        Assert.Equal(HttpStatusCode.Found, secondConfirm.StatusCode);
        Assert.Contains("/confirmar-email?status=already_confirmed", secondConfirm.Headers.Location?.ToString());
    }

    [Fact]
    public async Task ConfirmEmailRedirect_InvalidOrMissingToken_Redirects302ToError()
    {
        var invalidResponse = await _client.GetAsync("/api/v1/auth/confirm-email-redirect?token=invalid.token");
        Assert.Equal(HttpStatusCode.Found, invalidResponse.StatusCode);
        Assert.Contains("/confirmar-email?status=error", invalidResponse.Headers.Location?.ToString());

        var missingResponse = await _client.GetAsync("/api/v1/auth/confirm-email-redirect");
        Assert.Equal(HttpStatusCode.Found, missingResponse.StatusCode);
        Assert.Contains("/confirmar-email?status=error", missingResponse.Headers.Location?.ToString());
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
