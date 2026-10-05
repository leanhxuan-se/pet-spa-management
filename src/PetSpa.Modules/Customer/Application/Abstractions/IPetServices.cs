using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Application.Abstractions
{
    public interface IPetServices
    {
        Task<Result<PetResponse>> GetByIdAsync(
            long id, 
            CancellationToken ct);

        Task<Result<List<PetResponse>>> GetAllByIdAsync(
            long id, 
            CancellationToken ct);

        Task<Result<PetResponse>> CreateAsync(
            CreatePetRequest req, 
            CancellationToken ct);

        Task<Result<PetResponse>> UpdateAsync(
            long id, 
            UpdatePetRequest req, 
            CancellationToken ct);
    }
}
