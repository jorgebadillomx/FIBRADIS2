using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SharedApiContracts.Auth;

namespace Api.Tests;

public class AuthRegisterTests : IAsyncLifetime
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
    public async Task Register_ValidUser_Returns200AndQueuesConfirmationEmail()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "nuevo@fibradis.mx",
            "Fuerte1!",
            "Apodo",
            "Google"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.Equal("Revisa tu email para confirmar tu cuenta.", body!.Message);

        Assert.Single(_factory.EmailService.Emails);
        var email = _factory.EmailService.Emails[0];
        Assert.Equal("nuevo@fibradis.mx", email.ToEmail);
        Assert.Contains("/api/v1/auth/confirm-email-redirect?token=", email.ConfirmationUrl);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.SqlServer.AppDbContext>();
        var stored = await db.Users.SingleAsync();
        Assert.False(stored.IsActive);
        Assert.Null(stored.EmailConfirmedAt);
        Assert.Null(stored.TrialEndsAt);
    }

    [Fact]
    public async Task ResendConfirmation_ValidUser_QueuesRedirectConfirmationEmail()
    {
        const string email = "reenviar@fibradis.mx";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            email,
            "Fuerte1!",
            null,
            null));
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        _factory.EmailService.Clear();

        var response = await _client.PostAsJsonAsync("/api/v1/auth/resend-confirmation", new { Email = email });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Single(_factory.EmailService.Emails);
        var resendEmail = _factory.EmailService.Emails[0];
        Assert.Equal(email, resendEmail.ToEmail);
        Assert.Contains("/api/v1/auth/confirm-email-redirect?token=", resendEmail.ConfirmationUrl);
    }

    [Fact]
    public async Task Register_DisposableEmail_Returns422WithCode()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "user@mailinator.com",
            "Fuerte1!",
            null,
            null));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("disposable_email", body.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var first = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "dup@fibradis.mx",
            "Fuerte1!",
            null,
            null));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(
            "dup@fibradis.mx",
            "Fuerte2@",
            null,
            null));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidHowDidYouHear_Returns400WithCode()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            Email = "valid@fibradis.mx",
            Password = "Fuerte1!",
            Apodo = (string?)null,
            HowDidYouHear = "ValorInvalidoXYZ",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("invalid_user_data", body.GetProperty("code").GetString());
    }
}
