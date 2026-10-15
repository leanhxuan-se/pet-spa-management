using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Endpoints.Responses;
using PetSpa.Modules.Operation.Domain.Enums;
using Microsoft.EntityFrameworkCore;


namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories
{
    internal class StaffRepository(
        OperationDbContext db): IStaffRepository
    {
        public async Task<List<StaffResponse>> GetAvailableAsync(DateTime startTime, DateTime endTime, CancellationToken ct)
        {
            var availSession = await db.ServiceSessions
                .AsNoTracking()
                .Where(x =>
                    x.Status != ServiceSessionStatus.CANCELLED
                    && x.ExpectedStartAt <= endTime
                    && x.ExpectedFinishAt >= startTime)
                .SelectMany(x => x.StaffAssignments.Select(st => new StaffResponse(
                    st.Staff.Id,
                    st.Staff.FullName,
                    st.Staff.Position,
                    st.Staff.AvatarUrl)).ToList())
                .Distinct()
                .ToListAsync(ct);
            
            return availSession;
        }
    }
}
