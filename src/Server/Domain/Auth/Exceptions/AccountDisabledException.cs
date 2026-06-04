using Domain.Common;

namespace Domain.Auth.Exceptions;

public class AccountDisabledException()
    : DomainException("Tu cuenta está deshabilitada. Contacta al administrador.", "ACCOUNT_DISABLED");
