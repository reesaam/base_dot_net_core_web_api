using Dapper;
using BaseWebApi.Infrastructure.Configuration;
using BaseWebApi.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaseWebApi.Infrastructure.Persistence.Oracle;

/// <summary>
/// Placeholder Oracle access via Dapper.
/// TODO: Replace with Oracle.ManagedDataAccess.Client + Oracle.EntityFrameworkCore when Oracle is required.
/// </summary>
public interface IOracleRepository
{
    Task<IReadOnlyList<T>> QueryAsync<T>(string sql, object? param = null, CancellationToken cancellationToken = default);
}

public sealed class OracleRepository : IOracleRepository
{
    private readonly OracleOptions _options;
    private readonly ILogger<OracleRepository> _logger;

    public OracleRepository(IOptions<OracleOptions> options, ILogger<OracleRepository> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        object? param = null,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Oracle repository invoked while disabled.");
            return Task.FromResult<IReadOnlyList<T>>(Array.Empty<T>());
        }

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InfrastructureException(
                "Oracle connection string is not configured.",
                "oracle.connection");
        }

        // TODO: Open OracleConnection and execute with Dapper:
        // await using var connection = new OracleConnection(_options.ConnectionString);
        // var rows = await connection.QueryAsync<T>(new CommandDefinition(sql, param, cancellationToken: cancellationToken));
        _ = typeof(SqlMapper);
        throw new InfrastructureException(
            "Oracle provider is a placeholder. Add Oracle.ManagedDataAccess.Core to enable.",
            "oracle.not_implemented");
    }
}
