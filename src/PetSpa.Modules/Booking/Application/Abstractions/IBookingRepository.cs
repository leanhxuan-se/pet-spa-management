// Định nghĩa thao tác lưu trữ và truy vấn IBookingRepository.
using PetSpa.Modules.Booking.Domain.Entities;
using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Application.Abstractions;

public interface IBookingRepository
{
    // Tìm đối tượng theo mã.
    Task<BookingEntity?> GetByIdAsync(long id, CancellationToken ct);
    // Thêm đối tượng mới.
    void Add(BookingEntity booking);
    // Xóa chi tiết lịch hẹn.
    void RemoveDetail(BookingDetail detail);
    // Lưu thay đổi vào cơ sở dữ liệu.
    Task SaveChangesAsync(CancellationToken ct);
}
