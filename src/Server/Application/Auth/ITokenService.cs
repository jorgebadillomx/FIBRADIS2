using Domain.Auth;

namespace Application.Auth;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
}
