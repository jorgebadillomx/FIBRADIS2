using System.ComponentModel.DataAnnotations;

namespace SharedApiContracts.Auth;

public sealed record ChangeOwnPasswordRequest(
    [Required] string CurrentPassword,
    [Required] string NewPassword);
