using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Domain.Repositories;
using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Infrastructure.Persistence.Repositories;

public class CustomerRepository(CustomerDbContext db) : ICustomerRepository
{
    public Task<CustomerTable?> GetByPhoneAsync(string phone, CancellationToken ct)
    {
        return db.Customers.SingleOrDefaultAsync(x => x.Phone == phone, ct);
    }

    public async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
    }
}
