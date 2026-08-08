namespace BaseWebApi.Shared.Exceptions;

/// <summary>
/// Thrown when a domain invariant is violated.
/// </summary>
public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string message, string code = "domain.error")
        : base(message)
    {
        Code = code;
    }

    public DomainException(string message, Exception innerException, string code = "domain.error")
        : base(message, innerException)
    {
        Code = code;
    }
}
