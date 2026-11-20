using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Endpoints.Requests;
using Mapster;

using Microsoft.EntityFrameworkCore;
using PetSpa.SharedKernel.Application.Abstractions;



namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    
    internal class StaffRepository(OperationDbContext db, IPasswordService<Staff> pass): IStaffRepository
    {
        
        public async Task<List<StaffResponse>> GetAsync(CancellationToken ct)
        {
            var staffList = await db.Staffs
                .ProjectToType<StaffResponse>()
                .ToListAsync(ct);
            return staffList;
        }
        public async Task<StaffResponse> GetByIdAsync(long id, CancellationToken ct)
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
        public async Task<StaffResponse> UpdateAsync(long id, UpdateStaffRequest request, CancellationToken ct)
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
        public async Task<StaffResponse> DeleteAsync(long id, CancellationToken ct)
        {
            var staff = await db.Staffs.Where(x => x.Id == id).FirstOrDefaultAsync(ct);
            if (staff is null)
                return null;
            staff.Status = StaffStatus.INACTIVE;
            await db.SaveChangesAsync(ct);
            return staff.Adapt<StaffResponse>();
        }
        
        public async Task<List<StaffResponse>> GetUnAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            var unavailableStaff = await db.ServiceSessions
                .AsNoTracking()
                .Where(x =>
                    x.Status != ServiceSessionStatus.CANCELLED
                    && x.Status != ServiceSessionStatus.COMPLETED
                    && x.ExpectedStartAt < endTime
                    && x.ExpectedFinishAt > startTime)
                .SelectMany(x => x.StaffAssignments
                            .Where(st => st.Staff.Status != StaffStatus.INACTIVE)
                            .Select(st => new StaffResponse(
                                st.Staff.Id,
                                st.Staff.FullName,
                                st.Staff.Gender,
                                st.Staff.DateOfBirth,
                                st.Staff.Email,
                                st.Staff.Phone,
                                st.Staff.Position,
                                st.Staff.Role,
                                st.Staff.HiredDate,
                                st.Staff.AvatarUrl,
                                st.Staff.Note,
                                st.Staff.Status)))
                .Distinct()
                .ToListAsync(ct);

            return unavailableStaff;
        }
        public async Task<List<StaffResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            var unAvailableStaff = await db.ServiceSessions
                .AsNoTracking()
                .Where(x =>
                    x.Status != ServiceSessionStatus.CANCELLED
                    && x.ExpectedStartAt < endTime
                    && x.ExpectedFinishAt > startTime)
                .SelectMany(x => x.StaffAssignments
                            .Where(st => st.Staff.Status != StaffStatus.INACTIVE)
                            .Select(st => st.Staff.Id))
                .Distinct()
                .ToListAsync(ct);

            DateOnly date = DateOnly.FromDateTime(startTime);
            TimeOnly start = TimeOnly.FromDateTime(startTime);
            TimeOnly end = TimeOnly.FromDateTime(endTime);

            var AvailableStaff = await db.StaffShifts
                    .Where(x => x.Status == StaffShiftStatus.SCHEDULED
                           && x.Staff.Status == StaffStatus.ACTIVE
                           && x.StartTime < start
                           && x.EndTime > end
                           && x.ShiftDate == date
                           && !unAvailableStaff.Contains(x.Staff.Id))
                    .Select(st => new StaffResponse(
                                 st.Staff.Id,
                                st.Staff.FullName,
                                st.Staff.Gender,
                                st.Staff.DateOfBirth,
                                st.Staff.Email,
                                st.Staff.Phone,
                                st.Staff.Position,
                                st.Staff.Role,
                                st.Staff.HiredDate,
                                st.Staff.AvatarUrl,
                                st.Staff.Note,
                                st.Staff.Status))
                    .Distinct()
                    .ToListAsync(ct);

            return AvailableStaff;
        }
    }
}
