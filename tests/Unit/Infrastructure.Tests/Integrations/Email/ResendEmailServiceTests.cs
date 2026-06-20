using System.Net;
using System.Text.Json;
using Infrastructure.Integrations.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Integrations.Email;

public class ResendEmailServiceTests
{
    private static readonly ResendTemplateIds AllTemplates = new()
    {
        EmailConfirmation = "tmpl_confirm",
        PasswordReset = "tmpl_reset",
        PaymentNotification = "tmpl_payment",
        AccessExpired = "tmpl_expired",
        AccessActivated = "tmpl_activated",
        TrialExpiring = "tmpl_trial",
        SubscriptionExpiring = "tmpl_sub",
    };

    // ── Guard: configuración incompleta ──────────────────────────────────────

    [Fact]
    public async Task WhenApiKeyEmpty_DoesNotCallHttpAndDoesNotThrow()
    {
        var called = false;
        var service = CreateService(
            _ => { called = true; return Ok(); },
            apiKey: "");

        await service.SendEmailConfirmationAsync("u@test.com", "https://x.com", CancellationToken.None);

        Assert.False(called);
    }

    [Fact]
    public async Task WhenTemplateIdEmpty_DoesNotCallHttpAndDoesNotThrow()
    {
        var called = false;
        var service = CreateService(
            _ => { called = true; return Ok(); },
            templates: new ResendTemplateIds()); // todos vacíos

        await service.SendEmailConfirmationAsync("u@test.com", "https://x.com", CancellationToken.None);

        Assert.False(called);
    }

    // ── throwOnFailure ────────────────────────────────────────────────────────

    [Fact]
    public async Task WhenResendRejectsAndThrowOnFailureTrue_ThrowsHttpRequestException()
    {
        var service = CreateService(
            _ => Unprocessable(),
            templates: AllTemplates);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SendAccessExpiredAsync("u@test.com", CancellationToken.None));
    }

    [Fact]
    public async Task WhenResendRejectsAndThrowOnFailureFalse_DoesNotThrow()
    {
        var service = CreateService(
            _ => Unprocessable(),
            templates: AllTemplates);

        var ex = await Record.ExceptionAsync(
            () => service.SendEmailConfirmationAsync("u@test.com", "https://x.com", CancellationToken.None));

        Assert.Null(ex);
    }

    // ── Payload correcto ──────────────────────────────────────────────────────

    [Fact]
    public async Task SendEmailConfirmationAsync_SendsCorrectPayload()
    {
        string? capturedBody = null;
        var service = CreateService(async req =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return await Ok();
        });

        await service.SendEmailConfirmationAsync("user@test.com", "https://example.com/confirm", CancellationToken.None);

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        var root = doc.RootElement;

        Assert.Equal("noreply@test.com", root.GetProperty("from").GetString());
        Assert.Equal("user@test.com", root.GetProperty("to")[0].GetString());

        var tmpl = root.GetProperty("template");
        Assert.Equal("tmpl_confirm", tmpl.GetProperty("id").GetString());
        Assert.Equal("https://example.com/confirm",
            tmpl.GetProperty("variables").GetProperty("CONFIRMATION_URL").GetString());
    }

    [Fact]
    public async Task SendPasswordResetAsync_SendsCorrectPayloadAndPreview()
    {
        string? capturedBody = null;
        var service = CreateService(async req =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return await Ok();
        });

        await service.SendPasswordResetAsync("user@test.com", "https://example.com/reset", CancellationToken.None);

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        var root = doc.RootElement;

        Assert.Equal("user@test.com", root.GetProperty("to")[0].GetString());

        var template = root.GetProperty("template");
        Assert.Equal("tmpl_reset", template.GetProperty("id").GetString());

        var vars = template.GetProperty("variables");
        Assert.Equal("https://example.com/reset", vars.GetProperty("RESET_URL").GetString());
        Assert.Equal("60", vars.GetProperty("EXPIRY_MINUTES").GetString());
        Assert.Equal("Tienes 60 minutos para restablecer tu contraseña en Fibras Inmobiliarias.", vars.GetProperty("PREVIEW").GetString());
    }

    [Fact]
    public async Task SendPaymentNotificationAsync_SendsToContactEmailWithUserIdAndEmail()
    {
        string? capturedBody = null;
        var userId = Guid.NewGuid();
        var service = CreateService(async req =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return await Ok();
        });

        await service.SendPaymentNotificationAsync(userId, "payer@test.com", null, null, CancellationToken.None);

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        var root = doc.RootElement;

        Assert.Equal("contacto@fibrasinmobiliarias.com", root.GetProperty("to")[0].GetString());

        var vars = root.GetProperty("template").GetProperty("variables");
        Assert.Equal(userId.ToString(), vars.GetProperty("USER_ID").GetString());
        Assert.Equal("payer@test.com", vars.GetProperty("USER_EMAIL").GetString());
    }

    [Fact]
    public async Task SendTrialExpiringAsync_SendsCorrectDaysLeftAndActivationUrl()
    {
        string? capturedBody = null;
        var service = CreateService(async req =>
        {
            capturedBody = await req.Content!.ReadAsStringAsync();
            return await Ok();
        });

        await service.SendTrialExpiringAsync("u@test.com", daysLeft: 3, CancellationToken.None);

        Assert.NotNull(capturedBody);
        using var doc = JsonDocument.Parse(capturedBody);
        var vars = doc.RootElement.GetProperty("template").GetProperty("variables");

        Assert.Equal(3, vars.GetProperty("DAYS_LEFT").GetInt32());
        Assert.Equal("https://fibrasinmobiliarias.com/activar", vars.GetProperty("ACTIVATION_URL").GetString());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static ResendEmailService CreateService(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> factory,
        string apiKey = "test-api-key",
        string senderEmail = "noreply@test.com",
        ResendTemplateIds? templates = null)
    {
        var opts = Options.Create(new ResendOptions
        {
            ApiKey = apiKey,
            SenderEmail = senderEmail,
            Templates = templates ?? AllTemplates,
        });
        var httpClient = new HttpClient(new StubHttpMessageHandler(factory));
        return new ResendEmailService(httpClient, opts, NullLogger<ResendEmailService>.Instance);
    }

    private static Task<HttpResponseMessage> Ok()
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"id\":\"msg_abc\"}"),
        });

    private static Task<HttpResponseMessage> Unprocessable()
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.UnprocessableEntity)
        {
            Content = new StringContent("{\"name\":\"validation_error\",\"message\":\"Invalid template\"}"),
        });

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> factory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => factory(request);
    }
}
