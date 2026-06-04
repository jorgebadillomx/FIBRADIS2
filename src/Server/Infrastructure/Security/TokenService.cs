using Application.Auth;
using Domain.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Security;

public class TokenService(IConfiguration configuration) : ITokenService
{
    private const string SecretPlaceholder = "CHANGE-ME-IN-PRODUCTION-VIA-ENV-VAR-OR-SECRET-MANAGER!!";

    private readonly string _secret = configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException(
            "JWT Secret no configurado. Establecer Jwt:Secret en appsettings o variable de entorno Jwt__Secret.");
    private readonly string _issuer = configuration["Jwt:Issuer"] ?? "fibradis";
    private readonly string _audience = configuration["Jwt:Audience"] ?? "fibradis-client";
    private readonly int _accessTokenMinutes =
        int.Parse(configuration["Jwt:AccessTokenMinutes"] ?? "15");

    public string GenerateAccessToken(User user)
    {
        if (_secret == SecretPlaceholder)
            throw new InvalidOperationException(
                "Jwt:Secret está usando el placeholder por defecto. " +
                "Configure una clave segura mediante variables de entorno (Jwt__Secret) o Secret Manager.");

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("hasAcceptedTerms", user.HasAcceptedTerms ? "true" : "false"),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public string HashRefreshToken(string rawToken)
        => BCrypt.Net.BCrypt.HashPassword(rawToken);
}
