using System.Security.Cryptography;
using System.Text;
using Application.Auth;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Security;

public sealed class PasswordResetTokenService(IConfiguration configuration) : IPasswordResetTokenService
{
    private const string SecretPlaceholder = "CHANGE-ME-IN-PRODUCTION-VIA-ENV-VAR-OR-SECRET-MANAGER!!";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(60);
    private const int PasswordHashPrefixLength = 12;

    private readonly string _secret = GetSecret(configuration);

    public string GenerateToken(Guid userId, string passwordHash)
    {
        var payload = BuildPayload(userId);
        var signature = ComputeSignature(payload, BuildKey(passwordHash));
        return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{Base64UrlEncode(signature)}";
    }

    public Guid? TryDecodeUserId(string token)
    {
        if (!TryDecodePayload(token, out var payload))
            return null;

        var segments = payload.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2 || !Guid.TryParse(segments[0], out var userId))
            return null;

        return userId;
    }

    public PasswordResetTokenValidationResult ValidateToken(string token, string currentPasswordHash)
    {
        if (!TryDecodeToken(token, out var payload, out var signature))
            return PasswordResetTokenValidationResult.Invalid;

        var expectedSignature = ComputeSignature(payload, BuildKey(currentPasswordHash));
        if (expectedSignature.Length != signature.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedSignature, signature))
        {
            return PasswordResetTokenValidationResult.Invalid;
        }

        var segments = payload.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2 ||
            !Guid.TryParse(segments[0], out _))
        {
            return PasswordResetTokenValidationResult.Invalid;
        }

        if (!long.TryParse(segments[1], out var expiryUnix))
            return PasswordResetTokenValidationResult.Invalid;

        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiryUnix
            ? PasswordResetTokenValidationResult.Expired
            : PasswordResetTokenValidationResult.Valid;
    }

    private string BuildKey(string passwordHash)
    {
        var hashPrefix = string.IsNullOrWhiteSpace(passwordHash)
            ? string.Empty
            : passwordHash[..Math.Min(PasswordHashPrefixLength, passwordHash.Length)];

        return "fibradis-password-reset:" + _secret + hashPrefix;
    }

    private static string BuildPayload(Guid userId)
    {
        var expiryUnix = DateTimeOffset.UtcNow.Add(TokenLifetime).ToUnixTimeSeconds();
        return $"{userId}|{expiryUnix}";
    }

    private static byte[] ComputeSignature(string payload, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static string GetSecret(IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException(
                "JWT Secret no configurado. Establecer Jwt:Secret en appsettings o variable de entorno Jwt__Secret.");

        if (secret == SecretPlaceholder)
            throw new InvalidOperationException(
                "Jwt:Secret está usando el placeholder por defecto. Configure una clave segura mediante variables de entorno (Jwt__Secret) o Secret Manager.");

        return secret;
    }

    private static bool TryDecodeToken(string token, out string payload, out byte[] signature)
    {
        payload = string.Empty;
        signature = [];

        if (string.IsNullOrWhiteSpace(token))
            return false;

        if (!TryDecodePayload(token, out payload))
            return false;

        var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !TryDecodeBase64Url(parts[1], out signature))
            return false;

        return true;
    }

    private static bool TryDecodePayload(string token, out string payload)
    {
        payload = string.Empty;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !TryDecodeBase64Url(parts[0], out var payloadBytes))
            return false;

        payload = Encoding.UTF8.GetString(payloadBytes);
        return true;
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static bool TryDecodeBase64Url(string value, out byte[] data)
    {
        data = [];

        if (string.IsNullOrWhiteSpace(value))
            return false;

        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');

        try
        {
            data = Convert.FromBase64String(padded);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
