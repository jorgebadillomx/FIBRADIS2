namespace Domain.Common;

public abstract class DomainException(string message, string domainCode) : Exception(message)
{
    public string DomainCode { get; } = domainCode;
}
