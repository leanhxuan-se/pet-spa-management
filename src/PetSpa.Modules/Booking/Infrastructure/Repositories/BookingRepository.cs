using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Booking.Application.Abstractions;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Infrastructure.Persistence;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Infrastructure.Repositories;

public sealed class BookingRepository(BookingDbContext db) : IBookingRepository
{
    public Task<BookingEntity?> GetByIdAsync(long id, CancellationToken ct)
        => db.Bookings.Include(b => b.BookingDetails).FirstOrDefaultAsync(b => b.Id == id, ct);

    public void Add(BookingEntity booking) => db.Bookings.Add(booking);
    public void RemoveDetail(BookingDetail detail) => db.BookingDetails.Remove(detail);
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
