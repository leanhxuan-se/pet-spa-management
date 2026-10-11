using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Application.Abstractions
{
    public interface ICustomerService
    {
        Task<Result<CustomerResponse>> GetByIdAsync(
            long id,
            CancellationToken ct);

        Task<Result<CustomerResponse>> CreateAsync(
            CreateCustomerRequest req,
            CancellationToken ct);

        Task<Result<CustomerResponse>> UpdateAsync(
            long id,
            UpdateCustomerRequest req,
            CancellationToken ct);
    }
}