// Định nghĩa thao tác lưu trữ và truy vấn BookingRepository.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Booking.Application.Abstractions;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Infrastructure.Persistence;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Infrastructure.Repositories;

public sealed class BookingRepository(BookingDbContext db) : IBookingRepository
{
    // Tìm đối tượng theo mã.
    public Task<BookingEntity?> GetByIdAsync(long id, CancellationToken ct)
        => db.Bookings.Include(b => b.BookingDetails).FirstOrDefaultAsync(b => b.Id == id, ct);

    // Thêm đối tượng mới.
    public void Add(BookingEntity booking) => db.Bookings.Add(booking);
    // Xóa chi tiết lịch hẹn.
    public void RemoveDetail(BookingDetail detail) => db.BookingDetails.Remove(detail);
    // Lưu thay đổi vào cơ sở dữ liệu.
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
