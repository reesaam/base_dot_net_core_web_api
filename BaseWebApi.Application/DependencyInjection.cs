using BaseWebApi.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BaseWebApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IItemAppService, ItemAppService>();

        // TODO: Register additional feature services / handlers here as modules grow.
        return services;
    }
}
