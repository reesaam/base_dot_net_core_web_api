namespace BaseWebApi.Infrastructure.Configuration;

/// <summary>
/// Controls whether the API boots with zero external dependencies.
/// When UseLocalFallbacks is true (default for Development), SQL/Redis/Kafka/MinIO/Elastic
/// are replaced with in-memory / no-op implementations so `dotnet run` always works.
/// </summary>
public sealed class InfrastructureOptions
{
    public const string SectionName = "Infrastructure";

    /// <summary>
    /// true  = in-memory DB + memory cache + no-op Kafka/MinIO/Elastic (local-first).
    /// false = real external services (Staging/Production).
    /// null  = auto: true in Development, false otherwise.
    /// </summary>
    public bool? UseLocalFallbacks { get; set; }
}
