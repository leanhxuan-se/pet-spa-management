using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;
namespace PetSpa.Modules.Operation.Domain.Repositories;

internal interface IStaffAuthRepository
{
    Task<StaffTable?> GetByPhoneAsync(string phone, CancellationToken ct);

    Task SaveAsync(CancellationToken ct);
}
