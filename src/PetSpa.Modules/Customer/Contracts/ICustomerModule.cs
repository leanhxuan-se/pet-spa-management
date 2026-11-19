// Định nghĩa giao tiếp giữa các module: ICustomerModule.
using PetSpa.Modules.Customer.Contracts.Dtos;

namespace PetSpa.Modules.Customer.Contracts;

public interface ICustomerModule
{
    // Kiểm tra thú cưng thuộc khách hàng.
    Task<bool> PetBelongsToCustomerAsync(long petId, long customerId, CancellationToken ct);

    Task<BookingCustomerDto?> GetBookingCustomerAsync(
        long customerId, long petId, CancellationToken cancellationToken = default);

    Task<BookingContactDto?> GetBookingContactAsync(long customerId, CancellationToken ct = default);
}
