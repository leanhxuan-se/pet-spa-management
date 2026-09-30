using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Endpoints;


namespace PetSpa.Modules.Customer;

public static class CustomerModule
{
    public static IServiceCollection AddCustomerModule(this IServiceCollection services, string ConnectionString)
    {
        services.AddDbContext<CustomerDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {

                npgsql.MigrationsAssembly("PetSpa.Modules.Customer");

                // Seperate migrations history
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "customer");
            });
        });
        return services;
    }

    public static IEndpointRouteBuilder MapCustomerModuleEndpoint(this IEndpointRouteBuilder app) // Register Module's Endpoints
    {
        //...
        app.MapCustomerEndpoint();

        return app;
    }
}
