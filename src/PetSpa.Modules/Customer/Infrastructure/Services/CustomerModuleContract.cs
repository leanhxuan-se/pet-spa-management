using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Contracts;
using PetSpa.Modules.Customer.Infrastructure.Persistence;

namespace PetSpa.Modules.Customer.Infrastructure.Services;

internal sealed class CustomerModuleContract(CustomerDbContext db) : ICustomerModule
{
    public Task<bool> PetBelongsToCustomerAsync(long petId, long customerId, CancellationToken ct)
        => db.Pets.AsNoTracking().AnyAsync(
            p => p.Id == petId && p.CustomersId == customerId && p.Customer.Id == customerId, ct);
}
