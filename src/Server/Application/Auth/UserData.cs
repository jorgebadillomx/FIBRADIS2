namespace Application.Auth;

public sealed record UserData(Guid Id, string Email, string Role, bool IsActive, DateTime CreatedAt);
