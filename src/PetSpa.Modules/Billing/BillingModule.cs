// Đăng ký và kết nối các thành phần của BillingModule.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Billing.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Billing.Contracts;
using PetSpa.Modules.Billing.Infrastructure.Services;

namespace PetSpa.Modules.Billing;

public static class BillingModule
{
    // Đăng ký các dịch vụ của module thanh toán.
    public static IServiceCollection AddBillingModule(this IServiceCollection services, string ConnectionString)
    {
        services.AddDbContext<BillingDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                // Seperate migrations history
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "billing");
            });
        });
        services.AddScoped<IBillingModule, BillingModuleContract>();
        return services;
    } 
}
