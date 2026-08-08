using System.Text;
using BaseWebApi.Application.Abstractions.Caching;
using BaseWebApi.Application.Abstractions.Messaging;
using BaseWebApi.Application.Abstractions.Persistence;
using BaseWebApi.Application.Abstractions.Search;
using BaseWebApi.Application.Abstractions.Security;
using BaseWebApi.Application.Abstractions.Storage;
using BaseWebApi.Domain.Entities.Items;
using BaseWebApi.Infrastructure.Caching;
using BaseWebApi.Infrastructure.Configuration;
using BaseWebApi.Infrastructure.Fallbacks;
using BaseWebApi.Infrastructure.Messaging;
using BaseWebApi.Infrastructure.Persistence;
using BaseWebApi.Infrastructure.Persistence.Oracle;
using BaseWebApi.Infrastructure.Persistence.Repositories;
using BaseWebApi.Infrastructure.Search;
using BaseWebApi.Infrastructure.Security;
using BaseWebApi.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Minio;
using Nest;

namespace BaseWebApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<SqlServerOptions>(configuration.GetSection(SqlServerOptions.SectionName));
        services.Configure<OracleOptions>(configuration.GetSection(OracleOptions.SectionName));
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<ElasticsearchOptions>(configuration.GetSection(ElasticsearchOptions.SectionName));
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<ApiOptions>(configuration.GetSection(ApiOptions.SectionName));
        services.Configure<InfrastructureOptions>(configuration.GetSection(InfrastructureOptions.SectionName));

        var useLocalFallbacks = ResolveUseLocalFallbacks(configuration, environment);
        services.AddSingleton(new InfrastructureRuntime(useLocalFallbacks));

        if (useLocalFallbacks)
        {
            AddLocalFallbacks(services);
        }
        else
        {
            AddPersistence(services, configuration);
            AddRedis(services, configuration);
            AddMinio(services, configuration);
            AddKafka(services);
            AddElasticsearch(services, configuration);
            AddExternalHealthChecks(services, configuration);
        }

        AddSecurity(services, configuration);
        services.AddScoped<IOracleRepository, OracleRepository>();

        // Always expose a lightweight self check so /health works even with fallbacks.
        services.AddHealthChecks().AddCheck("self", () =>
            Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API is running"));

        return services;
    }

    public static bool ResolveUseLocalFallbacks(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration.GetSection(InfrastructureOptions.SectionName)
            .Get<InfrastructureOptions>()?.UseLocalFallbacks;

        if (configured.HasValue)
        {
            return configured.Value;
        }

        // Zero-config first run: Development defaults to local fallbacks.
        return environment.IsDevelopment();
    }

    private static void AddLocalFallbacks(IServiceCollection services)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase("BaseWebApi_Local"));

        services.AddDistributedMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IEventPublisher, NoOpEventPublisher>();
        services.AddSingleton<IObjectStorage, InMemoryObjectStorage>();
        services.AddSingleton<ISearchService, NoOpSearchService>();

        services.AddScoped(typeof(BaseWebApi.Application.Abstractions.Persistence.IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IEfRepository<>), typeof(EfRepository<>));
        services.AddScoped<IEfRepository<Item>, EfRepository<Item>>();
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var sql = configuration.GetSection(SqlServerOptions.SectionName).Get<SqlServerOptions>()
                  ?? new SqlServerOptions();

        // Short connect timeout so a missing SQL Server fails fast instead of hanging startup.
        var connectionString = EnsureConnectTimeout(sql.ConnectionString, seconds: 5);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(3);
                    sqlOptions.CommandTimeout(15);
                }));

        services.AddScoped(typeof(BaseWebApi.Application.Abstractions.Persistence.IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IEfRepository<>), typeof(EfRepository<>));
        services.AddScoped<IEfRepository<Item>, EfRepository<Item>>();
    }

    private static void AddRedis(IServiceCollection services, IConfiguration configuration)
    {
        var redis = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
                    ?? new RedisOptions();

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redis.Configuration;
            options.InstanceName = redis.InstanceName;
        });

        services.AddSingleton<ICacheService, RedisCacheService>();
    }

    private static void AddMinio(IServiceCollection services, IConfiguration configuration)
    {
        var minio = configuration.GetSection(MinioOptions.SectionName).Get<MinioOptions>()
                    ?? new MinioOptions();

        services.AddSingleton<IMinioClient>(_ =>
            new MinioClient()
                .WithEndpoint(minio.Endpoint)
                .WithCredentials(minio.AccessKey, minio.SecretKey)
                .WithSSL(minio.UseSsl)
                .Build());

        services.AddSingleton<IObjectStorage, MinioObjectStorage>();
    }

    private static void AddKafka(IServiceCollection services)
    {
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();
    }

    private static void AddElasticsearch(IServiceCollection services, IConfiguration configuration)
    {
        var elastic = configuration.GetSection(ElasticsearchOptions.SectionName).Get<ElasticsearchOptions>()
                      ?? new ElasticsearchOptions();

        services.AddSingleton<IElasticClient>(_ => ElasticSearchService.CreateClient(elastic));
        services.AddSingleton<ISearchService, ElasticSearchService>();
    }

    private static void AddSecurity(IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
                  ?? new JwtOptions();

        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        services.AddAuthorization();
    }

    private static void AddExternalHealthChecks(IServiceCollection services, IConfiguration configuration)
    {
        var sql = configuration.GetSection(SqlServerOptions.SectionName).Get<SqlServerOptions>()
                  ?? new SqlServerOptions();
        var redis = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
                    ?? new RedisOptions();
        var elastic = configuration.GetSection(ElasticsearchOptions.SectionName).Get<ElasticsearchOptions>()
                      ?? new ElasticsearchOptions();

        services.AddHealthChecks()
            .AddSqlServer(EnsureConnectTimeout(sql.ConnectionString, 3), name: "sqlserver", tags: ["db", "sql"])
            .AddRedis(redis.Configuration, name: "redis", tags: ["cache"])
            .AddElasticsearch(elastic.Url, name: "elasticsearch", tags: ["search"]);
    }

    private static string EnsureConnectTimeout(string connectionString, int seconds)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (connectionString.Contains("Connect Timeout", StringComparison.OrdinalIgnoreCase) ||
            connectionString.Contains("Connection Timeout", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        return connectionString.TrimEnd(';') + $";Connect Timeout={seconds}";
    }
}

/// <summary>Resolved runtime mode exposed to startup logging.</summary>
public sealed record InfrastructureRuntime(bool UseLocalFallbacks);
