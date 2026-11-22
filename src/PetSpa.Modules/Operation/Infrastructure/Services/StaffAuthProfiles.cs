using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Infrastructure.Persistence;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Operation.Infrastructure.Services;

internal sealed class StaffAuthProfiles(OperationDbContext db) : IStaffAuthProfiles
{
    public async Task<AccountProfile?> GetStaffForAuthAsync(long staffId, CancellationToken ct)
    {
        var staff = await db.Staffs.AsNoTracking().Where(x => x.Id == staffId)
            .Select(x => new { x.Id, x.FullName, x.Phone, x.Status, x.Role, x.AccessStamp }).SingleOrDefaultAsync(ct);
        return staff is null ? null : new(staff.Id, staff.FullName, staff.Phone,
            staff.Status == StaffStatus.ACTIVE, staff.Role.ToString().ToLowerInvariant(), staff.AccessStamp);
    }

    public Task<long[]> GetActiveAdminIdsAsync(CancellationToken ct) => db.Staffs.AsNoTracking()
        .Where(x => x.Status == StaffStatus.ACTIVE && x.Role == StaffRole.ADMIN)
        .Select(x => x.Id).ToArrayAsync(ct);
}
