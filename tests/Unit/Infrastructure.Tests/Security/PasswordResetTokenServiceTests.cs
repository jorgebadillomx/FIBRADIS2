using System.Security.Cryptography;
using System.Text;
using Application.Auth;
using Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Tests.Security;

public class PasswordResetTokenServiceTests
{
    private const string Secret = "test-secret-key-must-be-at-least-32-chars-long!!!";

    [Fact]
    public void GenerateAndValidateToken_ReturnsValidForSameUserAndPasswordHash()
    {
        var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var service = CreateService();
        var token = service.GenerateToken(userId, "bcrypt-hash-prefix-1234567890");

        var result = service.ValidateToken(token, "bcrypt-hash-prefix-1234567890");

        Assert.Equal(PasswordResetTokenValidationResult.Valid, result);
        Assert.Equal(userId, service.TryDecodeUserId(token));
    }

    [Fact]
    public void ValidateToken_ReturnsExpired_ForValidExpiredToken()
    {
        var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var passwordHash = "bcrypt-hash-prefix-1234567890";
        var token = CreateToken(userId, passwordHash, DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds());
        var service = CreateService();

        var result = service.ValidateToken(token, passwordHash);

        Assert.Equal(PasswordResetTokenValidationResult.Expired, result);
    }

    [Fact]
    public void ValidateToken_ReturnsInvalid_ForTamperedToken()
    {
        var service = CreateService();
        var token = service.GenerateToken(Guid.NewGuid(), "bcrypt-hash-prefix-1234567890");
        var parts = token.Split('.');
        var tampered = $"{parts[0]}.{(parts[1][0] == 'A' ? 'B' : 'A')}{parts[1][1..]}";

        var result = service.ValidateToken(tampered, "bcrypt-hash-prefix-1234567890");

        Assert.Equal(PasswordResetTokenValidationResult.Invalid, result);
    }

    [Fact]
    public void ValidateToken_ReturnsInvalid_WhenPasswordHashChangesAfterGeneration()
    {
        var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var service = CreateService();
        var token = service.GenerateToken(userId, "bcrypt-hash-prefix-1234567890");

        var result = service.ValidateToken(token, "different-bcrypt-hash-prefix-xyz");

        Assert.Equal(PasswordResetTokenValidationResult.Invalid, result);
    }

    [Fact]
    public void TryDecodeUserId_ReturnsUserIdFromTokenPayload()
    {
        var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var token = CreateToken(userId, "bcrypt-hash-prefix-1234567890", DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds());
        var service = CreateService();

        var decoded = service.TryDecodeUserId(token);

        Assert.Equal(userId, decoded);
    }

    private static PasswordResetTokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = Secret,
            })
            .Build();

        return new PasswordResetTokenService(config);
    }

    private static string CreateToken(Guid userId, string passwordHash, long expiryUnix)
    {
        var hashPrefix = passwordHash[..Math.Min(12, passwordHash.Length)];
        var payload = $"{userId}|{expiryUnix}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("fibradis-password-reset:" + Secret + hashPrefix));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
