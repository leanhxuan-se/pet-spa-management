using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using PetSpa.SharedKernel.Application.Abstractions;
using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Application.Abstractions
{
    public class PetServices(
        CustomerDbContext db,
        ServiceProvider serviceProvider): IPetServices
    {
        public async Task<Result<PetResponse>> GetByIdAsync(long userId, long petId, CancellationToken ct)
        {
            var pet = await db.Pets
                .Join()
                .FirstOrDefaultAsync(ct);
            if (pet is null)
            {
                return Result<PetResponse>.NotFound($"Pet with id {petId} not found for user {userId}");
            }
            var petResponse = pet.Adapt<PetResponse>();
            return Result<PetResponse>.Success(petResponse);
        }
        Task<Result<PetResponse>> GetAllPetsByIdAsync(long id, CancellationToken ct);
        Task<Result<PetResponse>> CreatePetAsync(CreatePetRequest req, CancellationToken ct);
        Task<Result<PetResponse>> UpdatePetAsync(UpdatePetRequest req, long petId, CancellationToken ct);


    }
}
