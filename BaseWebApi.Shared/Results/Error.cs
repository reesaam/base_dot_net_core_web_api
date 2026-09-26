namespace BaseWebApi.Shared.Results;

/// <summary>
/// Immutable error descriptor used by Result&lt;T&gt;.
/// </summary>
public sealed record Error(string Code, string Message, string? Details = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Validation(string message, string? details = null) =>
        new("validation.error", message, details);

    public static Error NotFound(string message, string? details = null) =>
        new("not_found", message, details);

    public static Error Conflict(string message, string? details = null) =>
        new("conflict", message, details);

    public static Error Unauthorized(string message = "Unauthorized", string? details = null) =>
        new("unauthorized", message, details);

    public static Error Forbidden(string message = "Forbidden", string? details = null) =>
        new("forbidden", message, details);

    public static Error Failure(string message, string? details = null) =>
        new("failure", message, details);
}