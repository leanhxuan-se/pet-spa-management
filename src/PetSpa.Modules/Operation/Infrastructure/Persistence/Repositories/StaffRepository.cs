using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Endpoints.Requests;
using Mapster;

using Microsoft.EntityFrameworkCore;
using PetSpa.SharedKernel.Application.Authentication;



namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    
    internal class StaffRepository(OperationDbContext db): IStaffRepository
    {
        
        public async Task<List<StaffResponse>> GetAsync(CancellationToken ct)
        {
            var staffList = await db.Staffs
                .ProjectToType<StaffResponse>()
                .ToListAsync(ct);
            return staffList;
        }
        public async Task<StaffResponse?> GetByIdAsync(long id, CancellationToken ct)
        {
            var staff = await db.Staffs
                .AsNoTracking()
                .Where(st => st.Id == id)
                .ProjectToType<StaffResponse>()
                .FirstOrDefaultAsync(ct);
            return staff;
        }
        public async Task<StaffResponse> CreateAsync(CreateStaffRequest request, CancellationToken ct)
        {
            var staff = new Staff
            {
                FullName = request.FullName,
                Gender = request.Gender,
                DateOfBirth = request.DateOfBirth,
                Email = request.Email,
                Phone = AccountPhone.Normalize(request.Phone),
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
            staff.Phone = request.Phone is null ? staff.Phone : AccountPhone.Normalize(request.Phone);
            staff.Position = request.Position ?? staff.Position;
            staff.HiredDate = request.HiredDate ?? staff.HiredDate;
            staff.AvatarUrl = request.AvatarUrl ?? staff.AvatarUrl;
            staff.Note = request.Note ?? staff.Note;
            UpdateAccess(staff, request.Role ?? staff.Role, request.Status ?? staff.Status);
            await db.SaveChangesAsync(ct);
            return staff.Adapt<StaffResponse>();
        }
        public async Task<StaffResponse?> DeleteAsync(long id, CancellationToken ct)
        {
            var staff = await db.Staffs.Where(x => x.Id == id).FirstOrDefaultAsync(ct);
            if (staff is null)
                return null;
            UpdateAccess(staff, staff.Role, StaffStatus.INACTIVE);
            await db.SaveChangesAsync(ct);
            return staff.Adapt<StaffResponse>();
        }
        
        private static void UpdateAccess(Staff staff, StaffRole role, StaffStatus status)
        {
            if (staff.Role == role && staff.Status == status)
                return;

            staff.Role = role;
            staff.Status = status;
            staff.AccessStamp = Guid.NewGuid();
        }

        public Task<List<StaffResponse>> GetAvailabilityAsync(
            DateTime startUtc, DateTime? endUtc, bool available, CancellationToken ct)
        {
            var assignments = db.StaffAssignments.Where(assignment => assignment.IsActive
                && (assignment.ServiceSession.Status == ServiceSessionStatus.WAITING
                    || assignment.ServiceSession.Status == ServiceSessionStatus.IN_PROGRESS));
            var busyIds = endUtc is { } end
                ? assignments.Where(assignment => assignment.ServiceSession.ExpectedStartAt < end
                    && assignment.ServiceSession.ExpectedFinishAt > startUtc).Select(assignment => assignment.StaffId)
                : assignments.Where(assignment => assignment.ServiceSession.Status == ServiceSessionStatus.IN_PROGRESS
                    || (assignment.ServiceSession.ExpectedStartAt <= startUtc
                        && assignment.ServiceSession.ExpectedFinishAt > startUtc)).Select(assignment => assignment.StaffId);

            var staff = db.Staffs.AsNoTracking().Where(person => person.Status == StaffStatus.ACTIVE);
            if (!available)
                return staff.Where(person => busyIds.Contains(person.Id)).OrderBy(person => person.Id)
                    .ProjectToType<StaffResponse>().ToListAsync(ct);

            var shifts = db.StaffShifts.Where(shift => shift.Status == StaffShiftStatus.WORKING);
            if (endUtc is { } endOfRange)
            {
                var date = DateOnly.FromDateTime(startUtc);
                if (date != DateOnly.FromDateTime(endOfRange))
                    return Task.FromResult(new List<StaffResponse>());

                var startTime = TimeOnly.FromDateTime(startUtc);
                var endTime = TimeOnly.FromDateTime(endOfRange);
                shifts = db.StaffShifts.Where(shift =>
                    (shift.Status == StaffShiftStatus.SCHEDULED || shift.Status == StaffShiftStatus.WORKING)
                    && shift.ShiftDate == date && shift.StartTime <= startTime && shift.EndTime >= endTime);
            }

            return staff.Where(person => !busyIds.Contains(person.Id) && shifts.Any(shift => shift.StaffId == person.Id))
                .OrderBy(person => person.Id).ProjectToType<StaffResponse>().ToListAsync(ct);
        }
    }
}
