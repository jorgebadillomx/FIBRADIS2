using Domain.Common;

namespace Domain.Auth.Exceptions;

public class InvalidUserDataException(string message)
    : DomainException(message, "INVALID_USER_DATA");
