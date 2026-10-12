using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using PetSpa.Modules.Operation.Infrastructure.Persistence;
using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;


using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Endpoints;
using FluentValidation;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Domain.Repositories;
using PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories;
using PetSpa.Modules.Operation.Infrastructure.Services;

namespace PetSpa.Modules.Operation;

internal static class OperationModule
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

        services.AddScoped<IPasswordHasher<StaffTable>, PasswordHasher<StaffTable>>();
        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IPasswordService, IdentityPasswordService>();
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
