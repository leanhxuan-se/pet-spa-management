using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Application.Abstractions
{
    public interface IPetServices
    {
        Task<Result<PetResponse>> GetByIdAsync(long userId, long petId, CancellationToken ct);
        Task<Result<PetResponse>> GetAllByIdAsync(long id, CancellationToken ct);
        Task<Result<PetResponse>> CreateAsync(CreatePetRequest req, CancellationToken ct);
        Task<Result<PetResponse>> UpdateAsync(UpdatePetRequest req, long petId, CancellationToken ct);


    }
}
