using System.Security.Claims;

namespace BaseWebApi.Application.Abstractions.Security;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string email, IEnumerable<string> roles, IDictionary<string, string>? extraClaims = null);

    ClaimsPrincipal? ValidateToken(string token);
}
