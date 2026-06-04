namespace Application.Auth;

public interface IUserService
{
    Task<UserData> CreateUserAsync(string email, string password, CancellationToken ct = default);
    Task<IReadOnlyList<UserData>> GetAllUsersAsync(CancellationToken ct = default);
}
