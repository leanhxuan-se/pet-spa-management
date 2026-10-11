using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Domain.Repositories;
using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories;

public class StaffRepository(OperationDbContext db) : IStaffRepository
{
    public Task<StaffTable?> GetByPhoneAsync(string phone, CancellationToken ct)
    {
        return db.Staffs.SingleOrDefaultAsync(x => x.Phone == phone, ct);
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}
