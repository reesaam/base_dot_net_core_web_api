namespace BaseWebApi.Api.Config;

/// <summary>
/// API-layer composition helpers.
/// TODO: Split CORS / rate-limiting / versioning into dedicated config classes as needed.
/// </summary>
public static class ApiConfiguration
{
    public const string CorsPolicyName = "DefaultCors";
}
