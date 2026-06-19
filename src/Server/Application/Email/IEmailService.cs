namespace Application.Email;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string toEmail, string confirmationUrl, CancellationToken ct);

    Task SendPaymentNotificationAsync(Guid userId, CancellationToken ct);
}
