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

    public async Task SendEmailConfirmationAsync(string toEmail, string confirmationUrl, CancellationToken ct)
    {
        var resendOptions = options.Value;

        if (string.IsNullOrWhiteSpace(resendOptions.ApiKey) || string.IsNullOrWhiteSpace(resendOptions.SenderEmail))
        {
            logger.LogError("Resend no está configurado; se omite el envío del email de confirmación a {ToEmail}.", toEmail);
            return;
        }

        var payload = new
        {
            from = resendOptions.SenderEmail,
            to = new[] { toEmail },
            subject = "Confirma tu cuenta en Fibras Inmobiliarias",
            html = $"""
                <h2>Confirma tu email</h2>
                <p>Haz clic en el enlace para activar tu prueba gratuita de 14 días:</p>
                <p><a href="{confirmationUrl}">Confirmar mi cuenta</a></p>
                <p>Este enlace expira en 24 horas.</p>
                <p>Si no creaste una cuenta, ignora este mensaje.</p>
                """,
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ResendEmailsUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", resendOptions.ApiKey);
            request.Content = JsonContent.Create(payload);

            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Resend rechazó el email de confirmación a {ToEmail} con status {StatusCode}. Body: {Body}",
                toEmail,
                (int)response.StatusCode,
                responseBody);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogError(ex, "No se pudo enviar el email de confirmación a {ToEmail} mediante Resend.", toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado al enviar el email de confirmación a {ToEmail}.", toEmail);
        }
    }

    public async Task SendPaymentNotificationAsync(Guid userId, CancellationToken ct)
    {
        var resendOptions = options.Value;

        if (string.IsNullOrWhiteSpace(resendOptions.ApiKey) || string.IsNullOrWhiteSpace(resendOptions.SenderEmail))
        {
            logger.LogError("Resend no está configurado; se omite la notificación de pago para userId={UserId}.", userId);
            return;
        }

        var payload = new
        {
            from = resendOptions.SenderEmail,
            to = new[] { "portafoliodefibras@gmail.com" },
            subject = "Notificación de pago — Fibras Inmobiliarias",
            html = $"<p>El usuario <strong>{userId}</strong> ha marcado su pago como realizado.</p>",
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ResendEmailsUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", resendOptions.ApiKey);
            request.Content = JsonContent.Create(payload);

            using var response = await httpClient.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
                return;

            var responseBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError(
                "Resend rechazó la notificación de pago para userId={UserId} con status {StatusCode}. Body: {Body}",
                userId,
                (int)response.StatusCode,
                responseBody);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            logger.LogError(ex, "No se pudo enviar la notificación de pago para userId={UserId}.", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado al enviar notificación de pago para userId={UserId}.", userId);
        }
    }
}
