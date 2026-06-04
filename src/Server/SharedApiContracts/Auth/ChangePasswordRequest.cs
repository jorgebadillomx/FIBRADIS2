using System.ComponentModel.DataAnnotations;

namespace SharedApiContracts.Auth;

public sealed record ChangePasswordRequest([Required] string NewPassword);
