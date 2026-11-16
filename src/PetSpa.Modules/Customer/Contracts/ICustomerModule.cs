// Định nghĩa giao tiếp giữa các module: ICustomerModule.
namespace PetSpa.Modules.Customer.Contracts;

public interface ICustomerModule
{
    // Kiểm tra thú cưng thuộc khách hàng.
    Task<bool> PetBelongsToCustomerAsync(long petId, long customerId, CancellationToken ct);
}
