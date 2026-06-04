using Domain.Common;

namespace Domain.Auth.Exceptions;

public class UserNotFoundException() : DomainException("Usuario no encontrado.", "USER_NOT_FOUND");
