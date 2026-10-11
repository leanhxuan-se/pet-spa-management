using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Domain.Repositories;

public interface ICustomerRepository
{
    Task<CustomerTable?> GetByPhoneAsync(string phone, CancellationToken ct);

    Task SaveAsync(CancellationToken ct);
}
