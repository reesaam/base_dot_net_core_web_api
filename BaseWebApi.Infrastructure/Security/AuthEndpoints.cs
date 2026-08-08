using BaseWebApi.Application.Abstractions.Security;
using BaseWebApi.Infrastructure.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace BaseWebApi.Infrastructure.Security;

public static class AuthEndpoints
{
    /// <summary>
    /// Development helper to mint JWTs for testing [Authorize] endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapDevAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/token", (TokenRequest request, IJwtTokenService tokens, IOptions<JwtOptions> options) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return Results.BadRequest(new { error = "Email is required." });
            }

            var userId = request.UserId == Guid.Empty ? Guid.NewGuid() : request.UserId;
            var roles = request.Roles is { Count: > 0 } ? request.Roles : ["User"];
            var token = tokens.CreateToken(userId, request.Email, roles);

            return Results.Ok(new
            {
                access_token = token,
                token_type = "Bearer",
                expires_in_minutes = options.Value.ExpirationMinutes,
                user_id = userId
            });
        })
        .AllowAnonymous()
        .WithTags("Auth");

        return endpoints;
    }

    public sealed record TokenRequest(Guid UserId, string Email, List<string>? Roles);
}
