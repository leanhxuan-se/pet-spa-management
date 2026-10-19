using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    internal class StaffRepository(
        OperationDbContext db): IStaffRepository
    {
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
                                st.Staff.Position,
                                st.Staff.AvatarUrl)))
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
                                st.Staff.Position,
                                st.Staff.AvatarUrl))
                    .Distinct()
                    .ToListAsync(ct);
            
            return AvailableStaff;
        }
    }
}
