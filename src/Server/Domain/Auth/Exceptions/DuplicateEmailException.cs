using Domain.Common;

namespace Domain.Auth.Exceptions;

public class DuplicateEmailException()
    : DomainException("Ya existe una cuenta con ese correo electrónico.", "DUPLICATE_EMAIL");
