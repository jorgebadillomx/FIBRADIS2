using System.ComponentModel.DataAnnotations;

namespace SharedApiContracts.Auth;

public sealed record CreateUserRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password);
