using BaseWebApi.Api.Filters;
using BaseWebApi.Api.Grpc.Services;
using BaseWebApi.Api.Middleware;
using BaseWebApi.Application;
using BaseWebApi.Infrastructure;
using BaseWebApi.Infrastructure.Grpc;
using BaseWebApi.Infrastructure.Logging;
using BaseWebApi.Infrastructure.Persistence;
using BaseWebApi.Infrastructure.Security;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using ProtoBuf.Grpc.Server;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog(SerilogConfiguration.ConfigureSerilog);

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    });

    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = "BaseWebApi API",
                Version = "v1",
                Description = "Modular REST + gRPC enterprise starter"
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme."
            };

            return Task.CompletedTask;
        });
    });

    builder.Services.AddCodeFirstGrpc(config =>
    {
        config.ResponseCompressionLevel = System.IO.Compression.CompressionLevel.Fastest;
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultCors", policy =>
            policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
    });

    var app = builder.Build();

    var runtime = app.Services.GetRequiredService<InfrastructureRuntime>();
    if (runtime.UseLocalFallbacks)
    {
        Log.Warning(
            "Running with LOCAL FALLBACKS (in-memory DB/cache, no-op Kafka/MinIO/Elastic). " +
            "Set Infrastructure:UseLocalFallbacks=false when SQL Server and other services are available.");
    }

    // Auto-generate .proto from [Service] interfaces + DTOs (Development / explicit flag).
    if (app.Environment.IsDevelopment() ||
        string.Equals(Environment.GetEnvironmentVariable("GENERATE_PROTOS"), "true", StringComparison.OrdinalIgnoreCase))
    {
        try
        {
            var generatedDir = Path.Combine(app.Environment.ContentRootPath, "Grpc", "Generated");
            ProtoSchemaGenerator.WriteProtos(generatedDir);
            Log.Information("Generated gRPC proto schemas in {Path}", generatedDir);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Proto generation skipped");
        }
    }

    await EnsureDatabaseAsync(app, runtime.UseLocalFallbacks);

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsProduction())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTitle("BaseWebApi API");
            options.WithOpenApiRoutePattern("/openapi/{documentName}.json");
        });
        app.MapDevAuthEndpoints();
    }

    app.UseCors("DefaultCors");
    app.UseGrpcWeb(new GrpcWebOptions { DefaultEnabled = true });
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapGrpcService<ItemGrpcService>().EnableGrpcWeb();

    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
    app.MapGet("/", () => Results.Redirect("/scalar"));

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static async Task EnsureDatabaseAsync(WebApplication app, bool useLocalFallbacks)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbBootstrap");

    try
    {
        if (useLocalFallbacks)
        {
            // InMemory provider has no migrations; EnsureCreated is enough.
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("In-memory database ready");
            return;
        }

        if (await db.Database.CanConnectAsync())
        {
            try
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("SQL Server migrations applied");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "MigrateAsync failed; trying EnsureCreated");
                await db.Database.EnsureCreatedAsync();
            }
        }
        else
        {
            logger.LogWarning(
                "SQL Server is not reachable. API will start but data endpoints may fail. " +
                "Enable Infrastructure:UseLocalFallbacks=true for offline development.");
        }
    }
    catch (Exception ex)
    {
        // Never block startup because of database availability.
        logger.LogWarning(
            ex,
            "Database bootstrap skipped. API is still starting. " +
            "Use Infrastructure:UseLocalFallbacks=true for a fully offline local run.");
    }
}

public partial class Program;
