// Đăng ký và kết nối các thành phần của BookingModule.
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Routing;
using PetSpa.Modules.Booking.Application.Abstractions;
using PetSpa.Modules.Booking.Application.Services;
using PetSpa.Modules.Booking.Endpoints;
using PetSpa.Modules.Booking.Infrastructure.Repositories;

namespace PetSpa.Modules.Booking;

public static class BookingModule
{
    // Đăng ký các dịch vụ của module lịch hẹn.
    public static IServiceCollection AddBookingModule(this IServiceCollection services, string ConnectionString)
    {
        // 1. Đăng ký kết nối cơ sở dữ liệu.
        services.AddDbContext<BookingDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(BookingDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "booking");
            });
        });
        // 2. Đăng ký các dịch vụ cần sử dụng.
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<BookingService>();
        return services;
    }

    // Đăng ký các endpoint của module lịch hẹn.
    public static IEndpointRouteBuilder MapBookingModuleEndpoint(this IEndpointRouteBuilder app)
        => app.MapBookingEndpoints();
}
