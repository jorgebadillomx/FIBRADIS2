namespace Application.Email;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string toEmail, string confirmationUrl, CancellationToken ct);

    Task SendPasswordResetAsync(string toEmail, string resetUrl, CancellationToken ct);

    Task SendPaymentNotificationAsync(Guid userId, string userEmail, byte[]? fileContent, string? fileName, CancellationToken ct);

    Task SendAccessExpiredAsync(string toEmail, CancellationToken ct);

    Task SendAccessActivatedAsync(string toEmail, CancellationToken ct);

    Task SendTrialExpiringAsync(string toEmail, int daysLeft, CancellationToken ct);

    Task SendSubscriptionExpiringAsync(string toEmail, int daysLeft, CancellationToken ct);
}
