namespace SharedApiContracts.Auth;

public record RegisterRequest(string Email, string Password, string? Apodo, string? HowDidYouHear);
