using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Endpoints;
using FluentValidation;
using PetSpa.Modules.Customer.Application.Validators;
using Microsoft.AspNetCore.Identity;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Application.Services;
using PetSpa.Modules.Customer.Domain.Repositories;
using PetSpa.Modules.Customer.Infrastructure.Persistence.Repositories;
using PetSpa.Modules.Customer.Infrastructure.Services;
using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;


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
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Customer");
            });
        });
        // Find UpdateCustomerRequestValidator and regis all the class that inherit AbstractValidator<T>
        services.AddValidatorsFromAssemblyContaining<UpdateCustomerRequestValidator>();

        // Đăng ký các interface và dịch vụ liên quan đến CustomerModule
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPasswordHasher<CustomerTable>, PasswordHasher<CustomerTable>>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IPasswordService, IdentityPasswordService>();
        services.AddScoped<CustomerLoginService>();
        return services;
    }

    public static IEndpointRouteBuilder MapCustomerModuleEndpoint(this IEndpointRouteBuilder app) // Register Module's Endpoints
    {
        //...
        app.MapCustomerEndpoint();
        app.MapCustomerAuthEndpoints();

        return app;
    }
}
