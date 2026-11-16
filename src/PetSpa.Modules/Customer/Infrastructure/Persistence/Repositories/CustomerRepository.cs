// Định nghĩa thao tác lưu trữ và truy vấn CustomerRepository.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Domain.Repositories;
using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Infrastructure.Persistence.Repositories;

internal class CustomerRepository(CustomerDbContext db) : ICustomerRepository
{
    // Tìm tài khoản theo số điện thoại.
    public Task<CustomerTable?> GetByPhoneAsync(string phone, CancellationToken ct)
    {
        return db.Customers.SingleOrDefaultAsync(x => x.Phone == phone, ct);
    }

    // Lưu thay đổi vào cơ sở dữ liệu.
    public async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}
