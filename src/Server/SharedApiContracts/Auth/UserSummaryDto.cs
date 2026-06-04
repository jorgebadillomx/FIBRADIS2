namespace SharedApiContracts.Auth;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt);
