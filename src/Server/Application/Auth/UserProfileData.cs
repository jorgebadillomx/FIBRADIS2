namespace Application.Auth;

public sealed record UserProfileData(
    Guid Id,
    string Email,
    string Role,
    string? Apodo);
