// Định nghĩa thao tác lưu trữ và truy vấn IBookingRepository.
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Application.Dtos;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Application.Abstractions;

public interface IBookingRepository
{
    Task<BookingEntity?> GetByIdAsync(long id, CancellationToken ct);
    Task<BookingEntity?> GetForUpdateAsync(long id, CancellationToken ct);
    Task<IReadOnlyList<BookingEntity>> ReadPageAsync(
        BookingListQuery query, BookingCursor? after, int limit, CancellationToken ct);
    Task<T> InTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct);

    void Add(BookingEntity booking);
    void RemoveDetail(BookingDetail detail);
    Task SaveChangesAsync(CancellationToken ct);
}
