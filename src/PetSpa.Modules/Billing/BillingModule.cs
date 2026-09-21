using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Billing.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace PetSpa.Modules.Billing;

public static class BillingModule
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services, string ConnectionString)
    {
        services.AddDbContext<BillingDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                // Seperate migrations history
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Billing");
            });
        });
        return services;
    } 
}
