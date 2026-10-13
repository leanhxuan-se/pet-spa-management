using PetSpa.Modules.Customer.Contracts.Dtos;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Contracts;

public interface ICustomerContract
{
    Task<Result<CustomerDto>> GetCustomerByIdAsync(
    long id,
    CancellationToken ct);

    Task<Result<PetDto>> GetPetByIdAsync(
    long id,
    CancellationToken ct);

    Task<Result<List<PetDto>>> GetPetsByCustomerIdAsync(
        long id,
        CancellationToken ct);


}

