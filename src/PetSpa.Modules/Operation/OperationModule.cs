using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Infrastructure.Persistence;


using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Endpoints;
using FluentValidation;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Domain.Repositories;
using PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.SharedKernel.Application.Validators;

namespace PetSpa.Modules.Operation;

public static class OperationModule
{
    public static IServiceCollection AddOperationModule(this IServiceCollection services, string ConnectionString)
    {
        services.AddDbContext<OperationDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(OperationDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "operation");

            });
        });

        services.AddScoped<IValidator<StaffLoginRequest>, LoginRequestValidator<StaffLoginRequest>>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<StaffLoginService>();
        return services;
    }
     public static IEndpointRouteBuilder MapOperationModuleEndpoint(this IEndpointRouteBuilder app) // Register Operation's Endpoints
    {
        //...
       
        app.MapOperationAuthEndpoints();

        return app;
    }
}
