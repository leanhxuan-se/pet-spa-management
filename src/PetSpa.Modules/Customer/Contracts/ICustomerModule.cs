namespace PetSpa.Modules.Customer.Contracts;

public interface ICustomerModule
{
    Task<bool> PetBelongsToCustomerAsync(long petId, long customerId, CancellationToken ct);
}
