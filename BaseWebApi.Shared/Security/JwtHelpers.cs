using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BaseWebApi.Shared.Constants;

namespace BaseWebApi.Shared.Security;

/// <summary>
/// JWT claim extraction and token parsing helpers.
/// </summary>
public static class JwtHelpers
{
    public static ClaimsPrincipal? ReadPrincipalWithoutValidation(string token)
    {
        if (string.IsNullOrWhiteSpace(token))return null;

        var handler = new JwtSecurityTokenHandler();
        if (!handler.CanReadToken(token)) return null;

        var jwt = handler.ReadJwtToken(token);
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: AppConstants.Auth.BearerScheme);
        return new ClaimsPrincipal(identity);
    }

    public static string? GetClaim(ClaimsPrincipal? principal, string claimType) =>
        principal?.FindFirst(claimType)?.Value
        ?? principal?.FindFirst(claimType.ToLowerInvariant())?.Value;

    public static Guid? GetUserId(ClaimsPrincipal? principal)
    {
        var value = GetClaim(principal, ClaimTypes.NameIdentifier) ?? GetClaim(principal, JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static string? GetEmail(ClaimsPrincipal? principal) =>
        GetClaim(principal, ClaimTypes.Email)
        ?? GetClaim(principal, JwtRegisteredClaimNames.Email);

    public static IReadOnlyList<string> GetRoles(ClaimsPrincipal? principal) =>
        principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct().ToArray() ?? [];
}