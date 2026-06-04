using System.ComponentModel.DataAnnotations;

namespace SharedApiContracts.Auth;

public sealed record CreateUserRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password,
    [Required] string Role,
    decimal? Pago,
    DateTime? FechaPago);
