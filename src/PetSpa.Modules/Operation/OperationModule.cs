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
                // Seperate migrations history
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Operation");
            });
        });
        return services;
    }
}
