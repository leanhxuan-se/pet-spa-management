// Đăng ký và kết nối các thành phần của CustomerModule.
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Endpoints;
using FluentValidation;
using PetSpa.Modules.Customer.Application.Validators;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Application.Services;
using PetSpa.Modules.Customer.Infrastructure.Services;
using PetSpa.Modules.Customer.Contracts;
using PetSpa.SharedKernel.Application.Authentication;


namespace PetSpa.Modules.Customer;

public static class CustomerModule
{
    // Đăng ký các dịch vụ của module khách hàng.
    public static IServiceCollection AddCustomerModule(this IServiceCollection services, string ConnectionString)
    {
        // 1. Đăng ký kết nối cơ sở dữ liệu.
        services.AddDbContext<CustomerDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {

                npgsql.MigrationsAssembly("PetSpa.Modules.Customer");

                // Seperate migrations history
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "customer");
            });
        });
        // Find UpdateCustomerRequestValidator and regis all the class that inherit AbstractValidator<T>
        services.AddValidatorsFromAssemblyContaining<UpdateCustomerRequestValidator>(includeInternalTypes: true);
        // 2. Đăng ký các dịch vụ cần sử dụng.

        // Đăng ký các interface và dịch vụ liên quan đến CustomerModule
        services.AddScoped<ICustomerContract, CustomerContract>();
        services.AddScoped<IPetServices, PetServices>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerAuthProfiles, CustomerAuthProfiles>();
        services.AddScoped<ICustomerModule, CustomerModuleContract>();
        return services;
    }

    // Đăng ký các endpoint của module khách hàng.
    public static IEndpointRouteBuilder MapCustomerModuleEndpoint(this IEndpointRouteBuilder app) // Register Module's Endpoints
    {
        //....
        app.MapCustomerEndpoint();
        app.MapPetEndpoint();

        return app;
    }
}
