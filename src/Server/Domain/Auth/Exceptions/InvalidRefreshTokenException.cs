using Domain.Common;

namespace Domain.Auth.Exceptions;

public class InvalidRefreshTokenException()
    : DomainException("Refresh token inválido o expirado.", "INVALID_REFRESH_TOKEN");
