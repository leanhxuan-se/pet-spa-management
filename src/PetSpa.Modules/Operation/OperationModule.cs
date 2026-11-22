// Đăng ký và kết nối các thành phần của OperationModule.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Infrastructure.Persistence;

using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Operation.Endpoints;
using PetSpa.Modules.Operation.Application.Abstractions;

using PetSpa.Modules.Operation.Application.Services;
using PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories;
using PetSpa.Modules.Operation.Infrastructure.Services;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Operation;

public static class OperationModule
{
    // Đăng ký các dịch vụ của module vận hành.
    public static IServiceCollection AddOperationModule(this IServiceCollection services, string ConnectionString)
    {
        // 1. Đăng ký kết nối cơ sở dữ liệu.
        services.AddDbContext<OperationDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(OperationDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "operation");

            });
        });

        services.AddScoped<IStaffRepository, StaffRepository>();
        services.AddScoped<IStaffShiftRepository, StaffShiftRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IStaffAuthProfiles, StaffAuthProfiles>();
        services.AddScoped<IServiceSessionRepository, ServiceSessionRepository>();
        services.AddScoped<IStaffAssignmentRepository, StaffAssignmentRepository>();
        services.AddScoped<IStaffNotificationRepository, StaffNotificationRepository>();
        services.AddScoped<RoomService>();
        services.AddScoped<StaffService>();
        services.AddScoped<StaffShiftService>();
        services.AddScoped<ServiceSessionService>();
        services.AddScoped<ServiceSessionGroupService>();
        services.AddScoped<StaffNotificationService>();
        services.AddScoped<StaffAssignmentService>();
        return services;
    }
     // Đăng ký các endpoint của module vận hành.
     public static IEndpointRouteBuilder MapOperationModuleEndpoint(this IEndpointRouteBuilder app) // Register Operation's Endpoints
    {
        //...

        app.MapRoomEndpoint();
        app.MapStaffEndpoint();
        app.MapStaffShiftEndpoint();
        app.MapServiceSessionEndpoint();
        app.MapMeEndpoint();

        return app;
    }
}
