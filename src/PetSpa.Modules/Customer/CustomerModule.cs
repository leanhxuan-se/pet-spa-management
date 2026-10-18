using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Customer.Endpoints;
using FluentValidation;
using PetSpa.Modules.Customer.Application.Validators;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Application.Services;
using PetSpa.Modules.Customer.Domain.Repositories;
using PetSpa.Modules.Customer.Infrastructure.Persistence.Repositories;
using PetSpa.Modules.Customer.Infrastructure.Services;
using PetSpa.Modules.Customer.Contracts;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.SharedKernel.Application.Validators;


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
        services.AddScoped<IValidator<CustomerLoginRequest>, LoginRequestValidator<CustomerLoginRequest>>();

        // Đăng ký các interface và dịch vụ liên quan đến CustomerModule
        services.AddScoped<ICustomerContract, CustomerContract>();
        services.AddScoped<IPetServices, PetServices>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<CustomerLoginService>();
        services.AddScoped<ICustomerModule, CustomerModuleContract>();
        return services;
    }

    public static IEndpointRouteBuilder MapCustomerModuleEndpoint(this IEndpointRouteBuilder app) // Register Module's Endpoints
    {
        //....
        app.MapCustomerAuthEndpoints();
        app.MapCustomerEndpoint();
        app.MapPetEndpoint();

        return app;
    }
}
