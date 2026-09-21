using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Resource.Infrastructure.Persistence;

namespace PetSpa.Modules.Resource;

public static class ResourceModule
{
    public static IServiceCollection AddResourceModule(this IServiceCollection services, string ConnectionString)
    {
        services.AddDbContext<ResourceDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                // Seperate migrations history
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Resource");
            });
        });
        return services;
    }
}
