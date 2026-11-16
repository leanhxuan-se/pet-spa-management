// Định nghĩa thao tác lưu trữ và truy vấn ICustomerRepository.
using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Domain.Repositories;

internal interface ICustomerRepository
{
    // Tìm tài khoản theo số điện thoại.
    Task<CustomerTable?> GetByPhoneAsync(string phone, CancellationToken ct);

    // Lưu thay đổi vào cơ sở dữ liệu.
    Task SaveAsync(CancellationToken ct);
}
