// Khai báo hợp đồng truy xuất dữ liệu thông báo nội bộ cho nhân viên.
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Endpoints.Responses;

namespace PetSpa.Modules.Operation.Application.Abstractions
{
    // Giao diện làm việc với bảng operation.staff_notification.
    internal interface IStaffNotificationRepository
    {
        // Tìm thông báo theo mã (có tracking để cập nhật trạng thái đã đọc).
        Task<StaffNotification?> GetByIdAsync(long id, CancellationToken ct);

        // Kiểm tra thông báo đã được ghi trước đó theo khóa chống trùng lặp (không tracking).
        Task<StaffNotification?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct);

        // Lấy danh sách thông báo của một nhân viên, sắp xếp mới nhất trước (không tracking).
        Task<IReadOnlyList<StaffNotificationResponse>> GetByStaffIdAsync(long staffId, CancellationToken ct);

        // Lấy thông tin phiên dịch vụ kèm phòng và danh sách phân công để kiểm tra quyền nhắc việc (không tracking).
        Task<ServiceSession?> GetSessionWithAssignmentsAsync(long serviceSessionId, CancellationToken ct);

        // Thêm bản ghi thông báo mới vào DbContext.
        Task AddAsync(StaffNotification notification, CancellationToken ct);

        // Lưu thay đổi thông báo xuống cơ sở dữ liệu.
        Task SaveChangesAsync(CancellationToken ct);
    }
}
