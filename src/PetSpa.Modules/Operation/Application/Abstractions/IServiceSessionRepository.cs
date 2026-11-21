// Khai báo hợp đồng truy xuất và cập nhật dữ liệu phiên dịch vụ trong Operation.
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Endpoints.Responses;
using Microsoft.EntityFrameworkCore.Storage;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    // Giao diện làm việc với bảng operation.service_session và các ràng buộc liên quan.
    internal interface IServiceSessionRepository
    {
        Task<IDbContextTransaction> BeginWriteAsync(CancellationToken ct);
        Task<List<ServiceSession>> GetByBookingIdsAsync(long[] bookingIds, CancellationToken ct);
        Task<bool> HasSessionsForDetailsAsync(long[] bookingDetailIds, CancellationToken ct);
        Task<bool> HasRunningRoomSessionAsync(long roomId, CancellationToken ct);
        void AddRange(IReadOnlyCollection<ServiceSession> sessions);

        // Lấy phiên dịch vụ theo Id kèm phòng và danh sách phân công (có tracking để cập nhật).
        Task<ServiceSession?> GetByIdWithDetailsAsync(long id, CancellationToken ct);

        // Đọc chi tiết phiên dịch vụ theo Id kèm phòng và phân công (không tracking phục vụ xem chi tiết).
        Task<ServiceSession?> GetByIdReadOnlyAsync(long id, CancellationToken ct);

        // Lấy danh sách phiên dịch vụ có phân công còn hiệu lực của nhân viên đang đăng nhập.
        Task<IReadOnlyList<MyServiceSessionResponse>> GetActiveAssignedSessionsByStaffIdAsync(long staffId, CancellationToken ct);

        // Lấy thông tin phòng theo Id để kiểm tra khả dụng khi điều chỉnh phiên.
        Task<Room?> GetRoomByIdAsync(long roomId, CancellationToken ct);

        // Kiểm tra phòng đích có trùng lịch với phiên hiệu lực khác ngoài excludeSessionId hay không.
        Task<bool> HasOverlappingRoomSessionAsync(long roomId, DateTime startUtc, DateTime endUtc, long excludeSessionId, CancellationToken ct);

        // Lọc ra các nhân viên trong danh sách có ca làm (SCHEDULED/WORKING) bao phủ trọn khoảng giờ mới.
        Task<IReadOnlyList<long>> GetStaffIdsWithCoveringShiftAsync(IReadOnlyCollection<long> staffIds, DateOnly shiftDate, TimeOnly startTime, TimeOnly endTime, CancellationToken ct);

        // Lọc ra các nhân viên trong danh sách đang có phân công hiệu lực khác trùng khung giờ mới.
        Task<IReadOnlyList<long>> GetStaffIdsWithOverlappingAssignmentAsync(IReadOnlyCollection<long> staffIds, DateTime startUtc, DateTime endUtc, long excludeSessionId, CancellationToken ct);

        // Lưu thay đổi phiên dịch vụ xuống cơ sở dữ liệu.
        Task SaveChangesAsync(CancellationToken ct);
    }
}
