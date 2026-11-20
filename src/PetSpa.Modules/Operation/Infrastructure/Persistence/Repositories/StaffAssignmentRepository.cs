// Cài đặt truy xuất dữ liệu EF Core cho phân công nhân viên trên phiên dịch vụ.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    // Repository quản lý phân công nhân viên, kiểm tra ca làm/trùng lịch và điều phối transaction.
    internal class StaffAssignmentRepository(OperationDbContext db) : IStaffAssignmentRepository
    {
        // Đọc phiên dịch vụ kèm thông tin phòng và danh sách phân công (kèm nhân viên) để phục vụ kiểm tra và cập nhật.
        public async Task<ServiceSession?> GetSessionWithAssignmentsAsync(long serviceSessionId, CancellationToken ct)
        {
            return await db.ServiceSessions
                .Include(s => s.Room)
                .Include(s => s.StaffAssignments)
                    .ThenInclude(sa => sa.Staff)
                .FirstOrDefaultAsync(s => s.Id == serviceSessionId, ct);
        }

        // Lấy hồ sơ nhân viên (không tracking) để kiểm tra tồn tại và trạng thái hoạt động.
        public async Task<Staff?> GetStaffByIdAsync(long staffId, CancellationToken ct)
        {
            return await db.Staffs
                .AsNoTracking()
                .FirstOrDefaultAsync(st => st.Id == staffId, ct);
        }

        // Kiểm tra có ca làm SCHEDULED hoặc WORKING của nhân viên trong ngày shiftDate bao phủ trọn [startTime, endTime].
        public async Task<bool> HasCoveringShiftAsync(
            long staffId,
            DateOnly shiftDate,
            TimeOnly startTime,
            TimeOnly endTime,
            CancellationToken ct)
        {
            return await db.StaffShifts
                .AsNoTracking()
                .AnyAsync(sh =>
                    sh.StaffId == staffId
                    && (sh.Status == StaffShiftStatus.SCHEDULED || sh.Status == StaffShiftStatus.WORKING)
                    && sh.ShiftDate == shiftDate
                    && sh.StartTime <= startTime
                    && sh.EndTime >= endTime,
                    ct);
        }

        // Kiểm tra phân công hiệu lực khác của nhân viên có giao khoảng [startUtc, endUtc) trên phiên hiệu lực khác không.
        public async Task<bool> HasOverlappingActiveAssignmentAsync(
            long staffId,
            DateTime startUtc,
            DateTime endUtc,
            long excludeSessionId,
            CancellationToken ct)
        {
            return await db.StaffAssignments
                .AsNoTracking()
                .AnyAsync(sa =>
                    sa.StaffId == staffId
                    && sa.IsActive
                    && sa.ServiceSessionId != excludeSessionId
                    && sa.ServiceSession.Status != ServiceSessionStatus.CANCELLED
                    && sa.ServiceSession.Status != ServiceSessionStatus.COMPLETED
                    && sa.ServiceSession.ExpectedStartAt != null
                    && sa.ServiceSession.ExpectedFinishAt != null
                    && sa.ServiceSession.ExpectedStartAt < endUtc
                    && sa.ServiceSession.ExpectedFinishAt > startUtc,
                    ct);
        }

        // Thêm bản ghi phân công mới vào DbContext.
        public async Task AddAsync(StaffAssignment assignment, CancellationToken ct)
        {
            await db.StaffAssignments.AddAsync(assignment, ct);
        }

        // Lưu thay đổi phân công xuống cơ sở dữ liệu.
        public async Task SaveChangesAsync(CancellationToken ct)
        {
            await db.SaveChangesAsync(ct);
        }

        // Mở database transaction bảo đảm ghi phân công và thông báo nội bộ thành công/rollback nguyên tử.
        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            var result = await action(ct);
            await transaction.CommitAsync(ct);
            return result;
        }
    }
}
