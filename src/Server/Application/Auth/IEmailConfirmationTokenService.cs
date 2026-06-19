namespace Application.Auth;

public sealed record EmailTokenValidationResult(Guid UserId, bool IsExpired, bool IsValid);

public interface IEmailConfirmationTokenService
{
    string GenerateToken(Guid userId);

    EmailTokenValidationResult ValidateToken(string token);
}
