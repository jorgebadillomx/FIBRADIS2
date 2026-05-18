using Domain.Common;

namespace Domain.Auth.Exceptions;

public class InvalidCredentialsException()
    : DomainException("Credenciales inválidas.", "INVALID_CREDENTIALS");
