namespace SharedApiContracts.Auth;

public sealed record UpdateSubscriptionRequest(
    string Type,
    DateTime StartedAt,
    DateTime? EndsAt);
