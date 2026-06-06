namespace Domain.Auth;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Apodo { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public bool HasAcceptedTerms { get; set; }
    public DateTime? TermsAcceptedAt { get; set; }
    public decimal? Pago { get; set; }
    public DateTime? FechaPago { get; set; }

    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
