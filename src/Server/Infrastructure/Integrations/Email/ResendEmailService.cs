using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Integrations.Email;

public sealed class ResendEmailService(
    HttpClient httpClient,
    IOptions<ResendOptions> options,
    ILogger<ResendEmailService> logger) : IEmailService
{
    private static readonly Uri ResendEmailsUri = new("https://api.resend.com/emails");
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new();
    private const string ContactEmail = "contacto@fibrasinmobiliarias.com";
    private const string SiteUrl = "https://fibrasinmobiliarias.com";

    public Task SendEmailConfirmationAsync(string toEmail, string confirmationUrl, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.EmailConfirmation,
            new { CONFIRMATION_URL = confirmationUrl },
            "email de confirmación",
            throwOnFailure: false,
            ct);

    public Task SendPaymentNotificationAsync(Guid userId, string userEmail, CancellationToken ct)
        => SendTemplatedEmailAsync(
            ContactEmail,
            options.Value.Templates.PaymentNotification,
            new { USER_ID = userId.ToString(), USER_EMAIL = userEmail },
            $"notificación de pago para userId={userId}",
            throwOnFailure: false,
            ct);

    public Task SendAccessExpiredAsync(string toEmail, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.AccessExpired,
            new { ACTIVATION_URL = $"{SiteUrl}/activar" },
            "aviso de acceso expirado",
            throwOnFailure: true,
            ct);

    public Task SendAccessActivatedAsync(string toEmail, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.AccessActivated,
            new { PORTFOLIO_URL = $"{SiteUrl}/portafolio" },
            "aviso de acceso activado",
            throwOnFailure: true,
            ct);

    public Task SendTrialExpiringAsync(string toEmail, int daysLeft, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.TrialExpiring,
            new { DAYS_LEFT = daysLeft, ACTIVATION_URL = $"{SiteUrl}/activar" },
            $"aviso de trial a {daysLeft} días",
            throwOnFailure: true,
            ct);

    public Task SendSubscriptionExpiringAsync(string toEmail, int daysLeft, CancellationToken ct)
        => SendTemplatedEmailAsync(
            toEmail,
            options.Value.Templates.SubscriptionExpiring,
            new { DAYS_LEFT = daysLeft, RENEWAL_URL = $"{SiteUrl}/activar" },
            $"aviso de suscripción a {daysLeft} días",
            throwOnFailure: true,
            ct);

    private async Task SendTemplatedEmailAsync(
        string toEmail,
        string templateId,
        object variables,
        string operation,
        bool throwOnFailure,
        CancellationToken ct)
    {
        var opt = options.Value;

        if (string.IsNullOrWhiteSpace(opt.ApiKey)
            || string.IsNullOrWhiteSpace(opt.SenderEmail)
            || string.IsNullOrWhiteSpace(templateId))
        {
            logger.LogError(
                "Resend no está configurado o falta template ID; se omite {Operation} a {ToEmail}.",
                operation, toEmail);
            return;
        }

        var payload = new
        {
            from = opt.SenderEmail,
            to = new[] { toEmail },
            template = new { id = templateId, variables }
        };

        Exception? failure = null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ResendEmailsUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opt.ApiKey);
            request.Content = JsonContent.Create(payload, options: JsonOptions);

            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Resend rechazó {Operation} a {ToEmail} con status {StatusCode}. Body: {Body}",
                operation, toEmail, (int)response.StatusCode, responseBody);

            if (throwOnFailure)
                failure = new HttpRequestException(
                    $"Resend rechazó {operation} a {toEmail} con status {(int)response.StatusCode}.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogError(ex, "No se pudo enviar {Operation} a {ToEmail} mediante Resend.", operation, toEmail);
            if (throwOnFailure)
                failure = ex is OperationCanceledException && !ct.IsCancellationRequested
                    ? new HttpRequestException($"Resend agotó el tiempo de espera al enviar {operation} a {toEmail}.", ex)
                    : ex;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado al enviar {Operation} a {ToEmail}.", operation, toEmail);
            if (throwOnFailure)
                failure = ex;
        }

        if (failure is not null)
            throw failure;
    }
}
