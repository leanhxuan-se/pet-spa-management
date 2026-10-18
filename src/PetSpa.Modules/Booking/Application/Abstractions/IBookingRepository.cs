using PetSpa.Modules.Booking.Domain.Entities;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Application.Abstractions;

public interface IBookingRepository
{
    Task<BookingEntity?> GetByIdAsync(long id, CancellationToken ct);
    void Add(BookingEntity booking);
    void RemoveDetail(BookingDetail detail);
    Task SaveChangesAsync(CancellationToken ct);
}
