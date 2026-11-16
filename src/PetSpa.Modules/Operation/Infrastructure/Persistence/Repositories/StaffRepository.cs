// Định nghĩa thao tác lưu trữ và truy vấn StaffRepository.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Domain.Repositories;
using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Repositories;

public class StaffRepository(OperationDbContext db) : IStaffRepository
{
    // Tìm tài khoản theo số điện thoại.
    public Task<StaffTable?> GetByPhoneAsync(string phone, CancellationToken ct)
    {
        return db.Staffs.SingleOrDefaultAsync(x => x.Phone == phone, ct);
    }

    // Lưu thay đổi vào cơ sở dữ liệu.
    public async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}
