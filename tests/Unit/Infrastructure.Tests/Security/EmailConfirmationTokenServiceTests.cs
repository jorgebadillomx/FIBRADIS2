using System.Security.Cryptography;
using System.Text;
using Application.Auth;
using Infrastructure.Security;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Tests.Security;

public class EmailConfirmationTokenServiceTests
{
    private const string Secret = "test-secret-key-must-be-at-least-32-chars-long!!!";

    [Fact]
    public void GenerateAndValidateToken_ReturnsSameUserId_AndNotExpired()
    {
        var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var service = CreateService();

        var token = service.GenerateToken(userId);
        var result = service.ValidateToken(token);

        Assert.True(result.IsValid);
        Assert.False(result.IsExpired);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public void ValidateToken_ReturnsExpired_ForValidExpiredToken()
    {
        var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var expiredUnix = DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds();
        var token = CreateToken(userId, expiredUnix);
        var service = CreateService();

        var result = service.ValidateToken(token);

        Assert.True(result.IsValid);
        Assert.True(result.IsExpired);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public void ValidateToken_ReturnsInvalid_ForTamperedToken()
    {
        var service = CreateService();
        var token = service.GenerateToken(Guid.NewGuid());
        var parts = token.Split('.');
        var tamperedSignature = $"{parts[0]}.{(parts[1][0] == 'A' ? 'B' : 'A')}{parts[1][1..]}";

        var result = service.ValidateToken(tamperedSignature);

        Assert.False(result.IsValid);
        Assert.False(result.IsExpired);
        Assert.Equal(Guid.Empty, result.UserId);
    }

    private static EmailConfirmationTokenService CreateService()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = Secret,
            })
            .Build();

        return new EmailConfirmationTokenService(config);
    }

    private static string CreateToken(Guid userId, long expiryUnix)
    {
        var payload = $"{userId}|{expiryUnix}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var signature = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
