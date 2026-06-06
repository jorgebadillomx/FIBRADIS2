namespace Application.Auth;

public interface IAuthService
{
    Task<(string AccessToken, string RefreshToken)> LoginAsync(
        string email, string password, CancellationToken ct = default);

    Task<(string AccessToken, string RefreshToken)> RefreshAsync(
        string rawRefreshToken, CancellationToken ct = default);

    Task LogoutAsync(string rawRefreshToken, CancellationToken ct = default);
}
