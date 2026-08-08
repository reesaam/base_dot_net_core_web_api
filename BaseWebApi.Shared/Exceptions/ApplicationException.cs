namespace BaseWebApi.Shared.Exceptions;

/// <summary>
/// Thrown for application/use-case failures.
/// </summary>
public class ApplicationException : Exception
{
    public string Code { get; }

    public ApplicationException(string message, string code = "application.error")
        : base(message)
    {
        Code = code;
    }

    public ApplicationException(string message, Exception innerException, string code = "application.error")
        : base(message, innerException)
    {
        Code = code;
    }
}
