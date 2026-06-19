using System.Security.Cryptography;
using System.Text;
using Application.Auth;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Security;

public sealed class EmailConfirmationTokenService(IConfiguration configuration) : IEmailConfirmationTokenService
{
    private const string SecretPlaceholder = "CHANGE-ME-IN-PRODUCTION-VIA-ENV-VAR-OR-SECRET-MANAGER!!";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly byte[] _secretBytes = GetSecretBytes(configuration);

    public string GenerateToken(Guid userId)
    {
        var expiryUnix = DateTimeOffset.UtcNow.Add(TokenLifetime).ToUnixTimeSeconds();
        var payload = $"{userId}|{expiryUnix}";
        var signature = ComputeSignature(payload);

        return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{Base64UrlEncode(signature)}";
    }

    public EmailTokenValidationResult ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new EmailTokenValidationResult(Guid.Empty, IsExpired: false, IsValid: false);

        var parts = token.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
            return new EmailTokenValidationResult(Guid.Empty, IsExpired: false, IsValid: false);

        if (!TryDecodeBase64Url(parts[0], out var payloadBytes) ||
            !TryDecodeBase64Url(parts[1], out var signatureBytes))
        {
            return new EmailTokenValidationResult(Guid.Empty, IsExpired: false, IsValid: false);
        }

        var payload = Encoding.UTF8.GetString(payloadBytes);
        var expectedSignature = ComputeSignature(payload);
        if (expectedSignature.Length != signatureBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedSignature, signatureBytes))
        {
            return new EmailTokenValidationResult(Guid.Empty, IsExpired: false, IsValid: false);
        }

        var payloadParts = payload.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (payloadParts.Length != 2 ||
            !Guid.TryParse(payloadParts[0], out var userId) ||
            !long.TryParse(payloadParts[1], out var expiryUnix))
        {
            return new EmailTokenValidationResult(Guid.Empty, IsExpired: false, IsValid: false);
        }

        var isExpired = DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= expiryUnix;
        return new EmailTokenValidationResult(userId, IsExpired: isExpired, IsValid: true);
    }

    private byte[] ComputeSignature(string payload)
    {
        using var hmac = new HMACSHA256(_secretBytes);
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
    }

    private static byte[] GetSecretBytes(IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException(
                "JWT Secret no configurado. Establecer Jwt:Secret en appsettings o variable de entorno Jwt__Secret.");

        if (secret == SecretPlaceholder)
            throw new InvalidOperationException(
                "Jwt:Secret está usando el placeholder por defecto. Configure una clave segura mediante variables de entorno (Jwt__Secret) o Secret Manager.");

        return Encoding.UTF8.GetBytes("fibradis-email-confirmation:" + secret);
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

    private static bool TryDecodeBase64Url(string value, out byte[] data)
    {
        data = Array.Empty<byte>();

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
