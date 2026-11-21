// Cài đặt truy xuất dữ liệu EF Core cho thông báo nội bộ nhân viên.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Endpoints.Responses;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    // Repository thao tác bảng staff_notification bằng OperationDbContext.
    internal class StaffNotificationRepository(OperationDbContext db) : IStaffNotificationRepository
    {
        // Tìm bản ghi thông báo theo Id (có tracking phục vụ đánh dấu đã đọc).
        public async Task<StaffNotification?> GetByIdAsync(long id, CancellationToken ct)
        {
            return await db.StaffNotifications
                .Include(n => n.StaffAssignment)
                .FirstOrDefaultAsync(n => n.Id == id, ct);
        }

        // Tra cứu thông báo theo IdempotencyKey để tránh tạo trùng khi client gửi lại.
        public async Task<StaffNotification?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken ct)
        {
            return await db.StaffNotifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.IdempotencyKey == idempotencyKey, ct);
        }

        // Truy vấn toàn bộ thông báo của nhân viên, mới nhất hiển thị trước.
        public async Task<IReadOnlyList<StaffNotificationResponse>> GetByStaffIdAsync(long staffId, CancellationToken ct)
        {
            return await db.StaffNotifications
                .AsNoTracking()
                .Where(n => n.StaffAssignment.StaffId == staffId)
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Select(n => new StaffNotificationResponse(
                    n.Id,
                    n.StaffAssignment.StaffId,
                    n.StaffAssignment.ServiceSessionId,
                    n.StaffAssignmentId,
                    n.Type,
                    n.Title,
                    n.Message,
                    n.IsRead,
                    n.ReadAt,
                    n.CreatedAt))
                .ToListAsync(ct);
        }

        // Đọc thông tin phiên kèm phòng và các phân công để kiểm tra quyền và xác định người nhận nhắc việc.
        public async Task<ServiceSession?> GetSessionWithAssignmentsAsync(long serviceSessionId, CancellationToken ct)
        {
            return await db.ServiceSessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.StaffAssignments)
                .FirstOrDefaultAsync(s => s.Id == serviceSessionId, ct);
        }

        // Thêm thông báo mới vào DbContext.
        public async Task AddAsync(StaffNotification notification, CancellationToken ct)
        {
            await db.StaffNotifications.AddAsync(notification, ct);
        }

        // Ghi thay đổi thông báo xuống cơ sở dữ liệu.
        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
