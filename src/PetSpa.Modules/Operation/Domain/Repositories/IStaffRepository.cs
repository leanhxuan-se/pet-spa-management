using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;

namespace PetSpa.Modules.Operation.Domain.Repositories;

public interface IStaffRepository
{
    Task<StaffTable?> GetByPhoneAsync(string phone, CancellationToken ct);

    Task SaveAsync(CancellationToken ct);
}
