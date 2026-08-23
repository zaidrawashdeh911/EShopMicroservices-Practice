using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Data;
using Ordering.Infrastructure.Data.Interceptors;

namespace Ordering.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add EF Core
        //services.AddDbContext<OrderingDbContext>(options =>
        //{
        //    options.UseSqlServer(configuration.GetConnectionString("OrderingConnectionString"));
        //});

        var connectionString = configuration.GetConnectionString("Database");

        // Add services to the container.
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());

            //options.AddInterceptors(new AuditableEntityInterceptor());
            //Wrong way because this needs to add MediatR in the parameter, and we'll implement MediatR in the Application Layer not Infrastructure Layer
            //, new DispatchDomainEventsInterceptor());


            options.UseSqlServer(connectionString);
        });


        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

        return services;
    }
}
