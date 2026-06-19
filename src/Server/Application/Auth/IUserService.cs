namespace Application.Auth;

public interface IUserService
{
    Task<UserData> CreateUserAsync(
        string email,
        string password,
        string role,
        decimal? pago = null,
        DateTime? fechaPago = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<UserData>> GetAllUsersAsync(CancellationToken ct = default);

    Task<UserData> SetUserActiveAsync(Guid id, bool isActive, CancellationToken ct = default);

    Task ChangePasswordAsync(Guid id, string newPassword, CancellationToken ct = default);

    Task<UserProfileData> GetProfileAsync(Guid userId, CancellationToken ct = default);

    Task UpdateApodoAsync(Guid userId, string? apodo, CancellationToken ct = default);

    Task ChangeOwnPasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default);

    Task<UserData> UpdatePaymentAsync(Guid id, decimal? pago, DateTime? fechaPago, CancellationToken ct = default);

    Task<UserData> UpdateSubscriptionAsync(Guid id, string type, DateTime startedAt, DateTime? endsAt, CancellationToken ct = default);

    Task AcceptTermsAsync(Guid userId, CancellationToken ct = default);
}
