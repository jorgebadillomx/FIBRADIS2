namespace SharedApiContracts.Auth;

public sealed record UserProfileResponse(
    string Email,
    string Role,
    string? Apodo,
    bool IsActive,
    string? TrialEndsAt,
    string? PaidAt);
