namespace SharedApiContracts.Auth;

public sealed record UserProfileResponse(
    string Email,
    string Role,
    string? Apodo);
