namespace Application.Auth;

public enum PasswordResetTokenValidationResult
{
    Valid,
    Invalid,
    Expired,
}

public interface IPasswordResetTokenService
{
    string GenerateToken(Guid userId, string passwordHash);

    Guid? TryDecodeUserId(string token);

    PasswordResetTokenValidationResult ValidateToken(string token, string currentPasswordHash);
}
