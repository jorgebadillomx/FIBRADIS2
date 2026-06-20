namespace Application.Auth;

public sealed record UserProfileData(
    Guid Id,
    string Email,
    string Role,
    string? Apodo,
    bool IsActive,
    DateTime? TrialEndsAt,
    DateTime? FechaPago,
    string? SubscriptionType,
    DateTime? SubscriptionEndsAt);
