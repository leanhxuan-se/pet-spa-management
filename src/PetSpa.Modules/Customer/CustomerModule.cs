using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Infrastructure.Persistence;

namespace PetSpa.Modules.Customer;

public static class CustomerModule
{
    public static IServiceCollection AddCustomerModule(this IServiceCollection services, string ConnectionString)
    {
        services.AddDbContext<CustomerDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                // Seperate migrations history
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Customer");
            });
        });
        return services;
    }
}
