using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace BaseWebApi.Infrastructure.Logging;

public static class SerilogConfiguration
{
    public static void ConfigureSerilog(HostBuilderContext context, LoggerConfiguration configuration)
    {
        var env = context.HostingEnvironment.EnvironmentName;
        var logPath = context.Configuration["Serilog:FilePath"] ?? $"logs/basewebapi-{env}-.log";

        configuration
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "BaseWebApi")
            .Enrich.WithProperty("Environment", env)
            .WriteTo.Console()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true)
            .ReadFrom.Configuration(context.Configuration);
    }
}
