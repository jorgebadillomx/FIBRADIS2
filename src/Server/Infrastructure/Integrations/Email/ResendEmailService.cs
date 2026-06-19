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
    private const string PublicSiteUrl = "https://fibrasinmobiliarias.com";

    public async Task SendEmailConfirmationAsync(string toEmail, string confirmationUrl, CancellationToken ct)
        => await SendEmailAsync(
            toEmail,
            "Confirma tu cuenta en Fibras Inmobiliarias",
            $"""
                <h2>Confirma tu email</h2>
                <p>Haz clic en el enlace para activar tu prueba gratuita de 14 días:</p>
                <p><a href="{confirmationUrl}">Confirmar mi cuenta</a></p>
                <p>Este enlace expira en 24 horas.</p>
                <p>Si no creaste una cuenta, ignora este mensaje.</p>
                """,
            "email de confirmación",
            throwOnFailure: false,
            ct);

    public async Task SendPaymentNotificationAsync(Guid userId, CancellationToken ct)
        => await SendEmailAsync(
            "portafoliodefibras@gmail.com",
            "Notificación de pago — Fibras Inmobiliarias",
            $"<p>El usuario <strong>{userId}</strong> ha marcado su pago como realizado.</p>",
            $"notificación de pago para userId={userId}",
            throwOnFailure: false,
            ct);

    public async Task SendAccessExpiredAsync(string toEmail, CancellationToken ct)
        => await SendEmailAsync(
            toEmail,
            "Tu acceso a Fibras Inmobiliarias ha expirado",
            $"""
                <p>Tu acceso ha expirado.</p>
                <p><a href="{PublicSiteUrl}/activar">Reactivar mi acceso</a></p>
                """,
            "aviso de acceso expirado",
            throwOnFailure: true,
            ct);

    public async Task SendAccessActivatedAsync(string toEmail, CancellationToken ct)
        => await SendEmailAsync(
            toEmail,
            "¡Tu acceso a Fibras Inmobiliarias está activo!",
            $"""
                <p>¡Tu acceso está activo! Bienvenido a Fibras Inmobiliarias.</p>
                <p><a href="{PublicSiteUrl}/portafolio">Ir a mi portafolio</a></p>
                """,
            "aviso de acceso activado",
            throwOnFailure: true,
            ct);

    public async Task SendTrialExpiringAsync(string toEmail, int daysLeft, CancellationToken ct)
        => await SendEmailAsync(
            toEmail,
            $"Tu prueba gratuita vence en {daysLeft} días",
            $"""
                <p>Tu prueba gratuita vence en {daysLeft} días.</p>
                <p><a href="{PublicSiteUrl}/activar">Ver planes y activar mi acceso</a></p>
                """,
            $"aviso de trial a {daysLeft} días",
            throwOnFailure: true,
            ct);

    public async Task SendSubscriptionExpiringAsync(string toEmail, int daysLeft, CancellationToken ct)
        => await SendEmailAsync(
            toEmail,
            $"Tu suscripción vence en {daysLeft} días",
            $"""
                <p>Tu suscripción vence en {daysLeft} días.</p>
                <p><a href="{PublicSiteUrl}/activar">Renovar mi acceso</a></p>
                """,
            $"aviso de suscripción a {daysLeft} días",
            throwOnFailure: true,
            ct);

    private async Task SendEmailAsync(
        string toEmail,
        string subject,
        string html,
        string operation,
        bool throwOnFailure,
        CancellationToken ct)
    {
        var resendOptions = options.Value;

        if (string.IsNullOrWhiteSpace(resendOptions.ApiKey) || string.IsNullOrWhiteSpace(resendOptions.SenderEmail))
        {
            logger.LogError("Resend no está configurado; se omite el envío de {Operation} a {ToEmail}.", operation, toEmail);
            if (throwOnFailure)
                throw new InvalidOperationException("Resend no está configurado.");

            return;
        }

        var payload = new
        {
            from = resendOptions.SenderEmail,
            to = new[] { toEmail },
            subject,
            html,
        };

        Exception? failure = null;

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
                "Resend rechazó {Operation} a {ToEmail} con status {StatusCode}. Body: {Body}",
                operation,
                toEmail,
                (int)response.StatusCode,
                responseBody);

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
