// Cài đặt truy xuất dữ liệu EF Core cho phiên dịch vụ trong Operation.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Responses;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    // Repository quản lý truy vấn và cập nhật bảng operation.service_session.
    internal class ServiceSessionRepository(OperationDbContext db) : IServiceSessionRepository
    {
        // Đọc phiên kèm phòng và toàn bộ phân công (kèm thông tin nhân viên) có tracking để cập nhật hoặc bắt đầu phiên.
        public async Task<ServiceSession?> GetByIdWithDetailsAsync(long id, CancellationToken ct)
        {
            return await db.ServiceSessions
                .Include(s => s.Room)
                .Include(s => s.StaffAssignments)
                    .ThenInclude(sa => sa.Staff)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        // Đọc chi tiết phiên dịch vụ không tracking để kiểm tra quyền truy cập và trả dữ liệu cho client.
        public async Task<ServiceSession?> GetByIdReadOnlyAsync(long id, CancellationToken ct)
        {
            return await db.ServiceSessions
                .AsNoTracking()
                .Include(s => s.Room)
                .Include(s => s.StaffAssignments)
                .FirstOrDefaultAsync(s => s.Id == id, ct);
        }

        // Truy vấn toàn bộ phiên dịch vụ đang gắn với phân công còn hiệu lực của nhân viên (OPS-11).
        public async Task<IReadOnlyList<MyServiceSessionResponse>> GetActiveAssignedSessionsByStaffIdAsync(long staffId, CancellationToken ct)
        {
            return await db.StaffAssignments
                .AsNoTracking()
                .Where(sa => sa.StaffId == staffId && sa.IsActive)
                .OrderBy(sa => sa.ServiceSession.ExpectedStartAt == null)
                .ThenBy(sa => sa.ServiceSession.ExpectedStartAt)
                .ThenBy(sa => sa.ServiceSessionId)
                .Select(sa => new MyServiceSessionResponse(
                    sa.Id,
                    sa.AssignedAt,
                    sa.ServiceSessionId,
                    sa.ServiceSession.BookingDetailId,
                    sa.ServiceSession.RoomId,
                    sa.ServiceSession.Room.RoomName,
                    sa.ServiceSession.Status,
                    sa.ServiceSession.ExpectedStartAt,
                    sa.ServiceSession.ExpectedFinishAt,
                    sa.ServiceSession.StartedAt,
                    sa.ServiceSession.FinishedAt))
                .ToListAsync(ct);
        }

        // Lấy thông tin phòng theo Id (không tracking).
        public async Task<Room?> GetRoomByIdAsync(long roomId, CancellationToken ct)
        {
            return await db.Rooms
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == roomId, ct);
        }

        // Kiểm tra phòng có phiên hiệu lực nào khác giao khoảng [startUtc, endUtc) hay không.
        public async Task<bool> HasOverlappingRoomSessionAsync(
            long roomId,
            DateTime startUtc,
            DateTime endUtc,
            long excludeSessionId,
            CancellationToken ct)
        {
            return await db.ServiceSessions
                .AsNoTracking()
                .AnyAsync(s =>
                    s.RoomId == roomId
                    && s.Id != excludeSessionId
                    && s.Status != ServiceSessionStatus.CANCELLED
                    && s.Status != ServiceSessionStatus.COMPLETED
                    && s.ExpectedStartAt != null
                    && s.ExpectedFinishAt != null
                    && s.ExpectedStartAt < endUtc
                    && s.ExpectedFinishAt > startUtc,
                    ct);
        }

        // Trả về các staffId có ca làm SCHEDULED hoặc WORKING bao phủ trọn [startTime, endTime] trong ngày shiftDate.
        public async Task<IReadOnlyList<long>> GetStaffIdsWithCoveringShiftAsync(
            IReadOnlyCollection<long> staffIds,
            DateOnly shiftDate,
            TimeOnly startTime,
            TimeOnly endTime,
            CancellationToken ct)
        {
            if (staffIds.Count == 0)
                return [];

            return await db.StaffShifts
                .AsNoTracking()
                .Where(sh =>
                    staffIds.Contains(sh.StaffId)
                    && (sh.Status == StaffShiftStatus.SCHEDULED || sh.Status == StaffShiftStatus.WORKING)
                    && sh.ShiftDate == shiftDate
                    && sh.StartTime <= startTime
                    && sh.EndTime >= endTime)
                .Select(sh => sh.StaffId)
                .Distinct()
                .ToListAsync(ct);
        }

        // Trả về các staffId đang có phân công còn hiệu lực trùng khoảng [startUtc, endUtc) trên phiên hiệu lực khác.
        public async Task<IReadOnlyList<long>> GetStaffIdsWithOverlappingAssignmentAsync(
            IReadOnlyCollection<long> staffIds,
            DateTime startUtc,
            DateTime endUtc,
            long excludeSessionId,
            CancellationToken ct)
        {
            if (staffIds.Count == 0)
                return [];

            return await db.StaffAssignments
                .AsNoTracking()
                .Where(sa =>
                    staffIds.Contains(sa.StaffId)
                    && sa.IsActive
                    && sa.ServiceSessionId != excludeSessionId
                    && sa.ServiceSession.Status != ServiceSessionStatus.CANCELLED
                    && sa.ServiceSession.Status != ServiceSessionStatus.COMPLETED
                    && sa.ServiceSession.ExpectedStartAt != null
                    && sa.ServiceSession.ExpectedFinishAt != null
                    && sa.ServiceSession.ExpectedStartAt < endUtc
                    && sa.ServiceSession.ExpectedFinishAt > startUtc)
                .Select(sa => sa.StaffId)
                .Distinct()
                .ToListAsync(ct);
        }

        // Lưu thay đổi phiên dịch vụ xuống cơ sở dữ liệu.
        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
