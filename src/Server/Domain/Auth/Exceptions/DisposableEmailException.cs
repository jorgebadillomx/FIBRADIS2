using Domain.Common;

namespace Domain.Auth.Exceptions;

public class DisposableEmailException()
    : DomainException("No se permiten correos desechables.", "DISPOSABLE_EMAIL");
