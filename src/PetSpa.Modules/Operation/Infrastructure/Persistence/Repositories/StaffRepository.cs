using Mapster;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Endpoints.Requests;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    internal class StaffRepository(OperationDbContext db, IPasswordService<Staff> pass) : IStaffRepository
    {
        public async Task<List<StaffResponse>> GetAsync(CancellationToken ct)
        {
            return await db.Staffs
                .AsNoTracking()
                .ProjectToType<StaffResponse>()
                .ToListAsync(ct);
        }

        public async Task<StaffResponse?> GetByIdAsync(long id, CancellationToken ct)
        {
            return await db.Staffs
                .AsNoTracking()
                .Where(st => st.Id == id)
                .ProjectToType<StaffResponse>()
                .FirstOrDefaultAsync(ct);
        }

        public async Task<StaffResponse> CreateAsync(CreateStaffRequest request, CancellationToken ct)
        {
            var staff = new Staff
            {
                FullName = request.FullName,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                Email = request.Email,
                Phone = request.Phone,
                PasswordHash = pass.HashPassword(new Staff(), request.Password),
                Position = request.Position,
                Role = request.Role,
                HiredDate = request.HiredDate,
                AvatarUrl = request.AvatarUrl,
                Note = request.Note,
                Status = request.Status
            };

            db.Staffs.Add(staff);
            await db.SaveChangesAsync(ct);
            return staff.Adapt<StaffResponse>();
        }

        public async Task<StaffResponse?> UpdateAsync(long id, UpdateStaffRequest request, CancellationToken ct)
        {
            var staff = await db.Staffs.Where(x => x.Id == id).FirstOrDefaultAsync(ct);
            if (staff is null)
                return null;

            staff.FullName = request.FullName ?? staff.FullName;
            staff.Gender = request.Gender ?? staff.Gender;
            staff.DateOfBirth = request.DateOfBirth ?? staff.DateOfBirth;
            staff.Email = request.Email ?? staff.Email;
            staff.Phone = request.Phone ?? staff.Phone;
            if (!string.IsNullOrWhiteSpace(request.Password))
                staff.PasswordHash = pass.HashPassword(staff, request.Password);
            staff.Position = request.Position ?? staff.Position;
            staff.Role = request.Role ?? staff.Role;
            staff.HiredDate = request.HiredDate ?? staff.HiredDate;
            staff.AvatarUrl = request.AvatarUrl ?? staff.AvatarUrl;
            staff.Note = request.Note ?? staff.Note;
            staff.Status = request.Status ?? staff.Status;
            await db.SaveChangesAsync(ct);
            return staff.Adapt<StaffResponse>();
        }

        public async Task<StaffResponse?> DeleteAsync(long id, CancellationToken ct)
        {
            var staff = await db.Staffs.Where(x => x.Id == id).FirstOrDefaultAsync(ct);
            if (staff is null)
                return null;

            staff.Status = StaffStatus.INACTIVE;
            await db.SaveChangesAsync(ct);
            return staff.Adapt<StaffResponse>();
        }

        public Task<List<StaffResponse>> GetAvailableAtAsync(DateTime nowUtc, CancellationToken ct)
        {
            // Tra rảnh hiện tại: nhân viên ACTIVE đang trong ca WORKING và chưa có phân công chiếm chỗ tại nowUtc.
            var query = db.Staffs
                .AsNoTracking()
                .Where(st =>
                    st.Status == StaffStatus.ACTIVE
                    && st.StaffShifts.Any(sh => sh.Status == StaffShiftStatus.WORKING)
                    && !st.StaffAssignments.Any(sa =>
                        sa.IsActive
                        && sa.ServiceSession.Status != ServiceSessionStatus.CANCELLED
                        && sa.ServiceSession.Status != ServiceSessionStatus.COMPLETED
                        && (sa.ServiceSession.Status == ServiceSessionStatus.IN_PROGRESS
                            || (sa.ServiceSession.ExpectedStartAt != null
                                && sa.ServiceSession.ExpectedFinishAt != null
                                && sa.ServiceSession.ExpectedStartAt <= nowUtc
                                && sa.ServiceSession.ExpectedFinishAt > nowUtc))));

            return ProjectStaffListAsync(query, ct);
        }

        public Task<List<StaffResponse>> GetUnAvailableAtAsync(DateTime nowUtc, CancellationToken ct)
        {
            // Tra bận hiện tại: nhân viên ACTIVE đang có phân công hiệu lực chiếm chỗ tại nowUtc.
            var query = db.Staffs
                .AsNoTracking()
                .Where(st =>
                    st.Status == StaffStatus.ACTIVE
                    && st.StaffAssignments.Any(sa =>
                        sa.IsActive
                        && sa.ServiceSession.Status != ServiceSessionStatus.CANCELLED
                        && sa.ServiceSession.Status != ServiceSessionStatus.COMPLETED
                        && (sa.ServiceSession.Status == ServiceSessionStatus.IN_PROGRESS
                            || (sa.ServiceSession.ExpectedStartAt != null
                                && sa.ServiceSession.ExpectedFinishAt != null
                                && sa.ServiceSession.ExpectedStartAt <= nowUtc
                                && sa.ServiceSession.ExpectedFinishAt > nowUtc))));

            return ProjectStaffListAsync(query, ct);
        }

        public Task<List<StaffResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            var startDate = DateOnly.FromDateTime(startTime);
            var endDate = DateOnly.FromDateTime(endTime);
            if (startDate != endDate)
                return Task.FromResult(new List<StaffResponse>());

            var start = TimeOnly.FromDateTime(startTime);
            var end = TimeOnly.FromDateTime(endTime);

            // Tra rảnh theo khoảng: ca SCHEDULED/WORKING bao phủ trọn khoảng và không trùng phân công hiệu lực.
            var query = db.Staffs
                .AsNoTracking()
                .Where(st =>
                    st.Status == StaffStatus.ACTIVE
                    && st.StaffShifts.Any(sh =>
                        (sh.Status == StaffShiftStatus.SCHEDULED || sh.Status == StaffShiftStatus.WORKING)
                        && sh.ShiftDate == startDate
                        && sh.StartTime <= start
                        && sh.EndTime >= end)
                    && !st.StaffAssignments.Any(sa =>
                        sa.IsActive
                        && sa.ServiceSession.Status != ServiceSessionStatus.CANCELLED
                        && sa.ServiceSession.Status != ServiceSessionStatus.COMPLETED
                        && sa.ServiceSession.ExpectedStartAt != null
                        && sa.ServiceSession.ExpectedFinishAt != null
                        && sa.ServiceSession.ExpectedStartAt < endTime
                        && sa.ServiceSession.ExpectedFinishAt > startTime));

            return ProjectStaffListAsync(query, ct);
        }

        public Task<List<StaffResponse>> GetUnAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            // Tra bận theo khoảng: nhân viên ACTIVE có phân công hiệu lực giao khoảng [startTime, endTime).
            var query = db.Staffs
                .AsNoTracking()
                .Where(st =>
                    st.Status == StaffStatus.ACTIVE
                    && st.StaffAssignments.Any(sa =>
                        sa.IsActive
                        && sa.ServiceSession.Status != ServiceSessionStatus.CANCELLED
                        && sa.ServiceSession.Status != ServiceSessionStatus.COMPLETED
                        && sa.ServiceSession.ExpectedStartAt != null
                        && sa.ServiceSession.ExpectedFinishAt != null
                        && sa.ServiceSession.ExpectedStartAt < endTime
                        && sa.ServiceSession.ExpectedFinishAt > startTime));

            return ProjectStaffListAsync(query, ct);
        }

        private static Task<List<StaffResponse>> ProjectStaffListAsync(IQueryable<Staff> query, CancellationToken ct)
        {
            return query
                .OrderBy(st => st.Id)
                .Select(st => new StaffResponse(
                    st.Id,
                    st.FullName,
                    st.Gender,
                    st.DateOfBirth,
                    st.Email,
                    st.Phone,
                    st.Position,
                    st.Role,
                    st.HiredDate,
                    st.AvatarUrl,
                    st.Note,
                    st.Status))
                .ToListAsync(ct);
        }
    }
}
