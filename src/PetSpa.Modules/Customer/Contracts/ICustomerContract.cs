// Định nghĩa giao tiếp giữa các module: ICustomerContract.
using PetSpa.Modules.Customer.Contracts.Dtos;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Contracts;

public interface ICustomerContract
{
    // Lấy thông tin khách hàng theo mã.
    Task<Result<CustomerDto>> GetCustomerByIdAsync(
    long id,
    CancellationToken ct);

    // Lấy thông tin thú cưng theo mã.
    Task<Result<PetDto>> GetPetByIdAsync(
    long id,
    CancellationToken ct);

    // Lấy thú cưng theo mã khách hàng.
    Task<Result<List<PetDto>>> GetPetsByCustomerIdAsync(
        long id,
        CancellationToken ct);


}

