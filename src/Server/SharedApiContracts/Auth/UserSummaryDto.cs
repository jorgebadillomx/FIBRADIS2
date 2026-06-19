namespace SharedApiContracts.Auth;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    decimal? Pago,
    DateTime? FechaPago,
    string? SubscriptionType,
    DateTime? SubscriptionStartedAt,
    DateTime? SubscriptionEndsAt,
    DateTime? TrialEndsAt,
    DateTime? EmailConfirmedAt);
