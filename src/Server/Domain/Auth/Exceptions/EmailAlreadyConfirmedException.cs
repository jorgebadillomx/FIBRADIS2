using Domain.Common;

namespace Domain.Auth.Exceptions;

public class EmailAlreadyConfirmedException()
    : DomainException("El correo electrónico ya fue confirmado.", "EMAIL_ALREADY_CONFIRMED");
