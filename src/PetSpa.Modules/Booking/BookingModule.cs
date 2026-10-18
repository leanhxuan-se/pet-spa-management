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
    public static IServiceCollection AddBookingModule(this IServiceCollection services, string ConnectionString)
    {
        services.AddDbContext<BookingDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(BookingDbContext).Assembly.FullName);
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "booking");
            });
        });
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<BookingService>();
        return services;
    }

    public static IEndpointRouteBuilder MapBookingModuleEndpoint(this IEndpointRouteBuilder app)
        => app.MapBookingEndpoints();
}
