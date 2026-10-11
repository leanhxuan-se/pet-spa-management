using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Infrastructure.Persistence;


namespace PetSpa.Modules.Operation;

public static class OperationModule
{
    public static IServiceCollection AddOperationModule(this IServiceCollection services, string ConnectionString)
    {
        services.AddDbContext<OperationDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
<<<<<<< Updated upstream
                // Seperate migrations history
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Operation");
=======
                npgsql.MigrationsAssembly(typeof(OperationDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "operation");
>>>>>>> Stashed changes
            });
        });
        return services;
    }
     public static IEndpointRouteBuilder MapOperationModuleEndpoint(this IEndpointRouteBuilder app) // Register Operation's Endpoints
    {
        //...
       
        app.MapOperationAuthEndpoints();

        return app;
    }
}
