using System.Net;
using System.Text.Json;
using BaseWebApi.Shared.Exceptions;
using BaseWebApi.Shared.Results;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ApplicationException = BaseWebApi.Shared.Exceptions.ApplicationException;

namespace BaseWebApi.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteErrorAsync(context, ex);
        }
    }

    private async Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        var (status, error) = Map(exception);
        _logger.LogError(exception, "Unhandled exception: {Code}", error.Code);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)status;

        var payload = new
        {
            error.Code,
            error.Message,
            Details = _environment.IsDevelopment()
                ? error.Details ?? exception.ToString()
                : error.Details
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    private static (HttpStatusCode Status, Error Error) Map(Exception exception) =>
        exception switch
        {
            ValidationException validation => (
                HttpStatusCode.BadRequest,
                Error.Validation(
                    "Validation failed.",
                    string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)))),
            DomainException domain => (
                HttpStatusCode.BadRequest,
                new Error(domain.Code, domain.Message)),
            ApplicationException app => (
                HttpStatusCode.BadRequest,
                new Error(app.Code, app.Message)),
            InfrastructureException infra => (
                HttpStatusCode.BadGateway,
                new Error(infra.Code, infra.Message)),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                Error.Unauthorized()),
            KeyNotFoundException notFound => (
                HttpStatusCode.NotFound,
                Error.NotFound(notFound.Message)),
            _ => (
                HttpStatusCode.InternalServerError,
                Error.Failure("An unexpected error occurred."))
        };
}
