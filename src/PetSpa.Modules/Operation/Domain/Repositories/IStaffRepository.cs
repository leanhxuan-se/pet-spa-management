// Định nghĩa thao tác lưu trữ và truy vấn IStaffRepository.
using StaffTable = PetSpa.Modules.Operation.Domain.Entities.Staff;

namespace PetSpa.Modules.Operation.Domain.Repositories;

public interface IStaffRepository
{
    // Tìm tài khoản theo số điện thoại.
    Task<StaffTable?> GetByPhoneAsync(string phone, CancellationToken ct);

    // Lưu thay đổi vào cơ sở dữ liệu.
    Task SaveAsync(CancellationToken ct);
}
