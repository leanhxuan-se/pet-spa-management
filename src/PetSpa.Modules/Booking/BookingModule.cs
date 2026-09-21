using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Booking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace PetSpa.Modules.Booking;

public static class BookingModule
{
    public static IServiceCollection AddBookingModule(this IServiceCollection services, string ConnectionString)
    {
        services.AddDbContext<BookingDbContext>(options =>
        {
            options.UseNpgsql(ConnectionString, npgsql =>
            {
                // Seperate migrations history
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "Booking");
            });
        });
        return services;
    }
}
