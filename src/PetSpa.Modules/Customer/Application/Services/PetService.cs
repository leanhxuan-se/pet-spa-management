using FluentValidation;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PetSpa.Modules.Customer.Domain.Entities;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Infrastructure.Persistence;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Application.Abstractions
{
    public class PetServices(
        CustomerDbContext db,
        ServiceProvider serviceProvider): IPetServices
    {
        public async Task<Result<PetResponse>> GetByIdAsync(long petId, CancellationToken ct)
        {
            var pet = await db.Pets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == petId, ct);
            if (pet is null)
            {
                return Result<PetResponse>.NotFound($"Pet with id {petId} not found");
            }
            var petResponse = pet.Adapt<PetResponse>();
            return Result<PetResponse>.Success(petResponse);
        }
        public async Task<Result<List<PetResponse>>> GetAllByIdAsync(long id, CancellationToken ct)
        {
            var pets = await db.Pets
                .Where(c => c.CustomersId == id)
                .AsNoTracking()
                .ToListAsync(ct);

            if (pets.Count == 0)
            {
                return Result<List<PetResponse>>.NotFound($" user {id} does not have any pet");
            }
            var petResponse = pets.Adapt<List<PetResponse>>();

            return Result<List<PetResponse>>.Success(petResponse);
        }
        public async Task<Result<PetResponse>> CreateAsync(CreatePetRequest req, CancellationToken ct)
        {
            var validator = serviceProvider.GetRequiredService<IValidator<CreatePetRequest>>();
            var validationResult = validator.Validate(req);
            if (!validationResult.IsValid)
            {
                return Result<PetResponse>.ValidationFailed(validationResult.ToDictionary());
            }

            var pet = new Pet
            {
                CustomersId = req.CustomersId,
                Name = req.Name,
                Species = req.Species,
                Gender = req.Gender,
                Weight = req.Weight,
                Height = req.Height,
                DateOfBirth = req.DateOfBirth,
                SpecialNote = req.SpecialNote,
                Status = req.Status
            };

            db.Pets.Add(pet);
            await db.SaveChangesAsync(ct);

            var petResponse = pet.Adapt<PetResponse>();
            return Result<PetResponse>.Success(petResponse);
        }

        public async Task<Result<PetResponse>> UpdateAsync(long petId, UpdatePetRequest req, CancellationToken ct)
        {
            var validator = serviceProvider.GetRequiredService<IValidator<UpdatePetRequest>>();
            var validationResult = validator.Validate(req);
            if (!validationResult.IsValid)
            {
                return Result<PetResponse>.ValidationFailed(validationResult.ToDictionary());
            }

            var pet = await db.Pets
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == petId, ct);
            if (pet is null)
            {
                return Result<PetResponse>.NotFound($"Pet with id {petId} not found");
            }
            pet.Name = req.Name ?? pet.Name;
            pet.Species = req.Species ?? pet.Species;
            pet.Gender = req.Gender ?? pet.Gender;
            pet.Weight = req.Weight ?? pet.Weight;
            pet.Height = req.Height ?? pet.Height;
            pet.DateOfBirth = req.DateOfBirth ?? pet.DateOfBirth;
            pet.SpecialNote = req.SpecialNote ?? pet.SpecialNote;
            pet.Status = req.Status ?? pet.Status;

            await db.SaveChangesAsync(ct);
            var petResponse = pet.Adapt<PetResponse>();

            return Result<PetResponse>.Success(petResponse);

        }
    }
}
