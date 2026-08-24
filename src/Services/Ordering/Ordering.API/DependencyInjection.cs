using BuildingBlocks.Exceptions.Handler;

namespace Ordering.API;

public static class DependencyInjection
{
    // This is an extension method for IServiceCollection to add API services
    // This is the part where its before the app is built, so we can add services to the DI container
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Add Carter
        services.AddCarter();

        services.AddExceptionHandler<CustomExceptionHandler>();

        // Add HealthChecks
        //services.AddHealthChecks();
        return services;
    }

    // This is an extension method for WebApplication to use API services
    // This is the part where its after the app is built, so we can use the services in the DI container
    public static WebApplication UseApiServices(this WebApplication app)
    {
        // Use Carter
        app.MapCarter();

        app.UseExceptionHandler(options => { });

        // Use HealthChecks
        //app.MapHealthChecks("/health");

        return app;
    }
}
