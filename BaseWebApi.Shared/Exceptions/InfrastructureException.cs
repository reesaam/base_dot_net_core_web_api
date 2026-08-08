namespace BaseWebApi.Shared.Exceptions;

/// <summary>
/// Thrown when an infrastructure dependency fails.
/// </summary>
public class InfrastructureException : Exception
{
    public string Code { get; }

    public InfrastructureException(string message, string code = "infrastructure.error")
        : base(message)
    {
        Code = code;
    }

    public InfrastructureException(string message, Exception innerException, string code = "infrastructure.error")
        : base(message, innerException)
    {
        Code = code;
    }
}
