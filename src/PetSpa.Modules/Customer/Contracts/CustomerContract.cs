using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Contracts.Dtos;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Contracts;

public class CustomerContract(ICustomerService customerService, IPetServices petServices) : ICustomerContract
{
    public async Task<Result<CustomerDto>> GetCustomerByIdAsync(long id, CancellationToken ct)
    {
        var customer = (await customerService.GetByIdAsync(id, ct)).Value;
        return customer is null
            ? Result<CustomerDto>.NotFound($"Customer with id {id} not found")
            : Result<CustomerDto>.Success(new CustomerDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Gender = customer.Gender?.ToString(),
                DateOfBirth = customer.DateOfBirth,
                Email = customer.Email,
                Phone = customer.Phone,
                AvtURL = customer.AvtURL,
                Note = customer.Note,
                Status = customer.Status,
                Pets = customer.Pets?.Select(ToPetDto).ToList()
            });
    }

    public async Task<Result<PetDto>> GetPetByIdAsync(long id, CancellationToken ct)
    {
        var pet = (await petServices.GetByIdAsync(id, ct)).Value;
        return pet is null
            ? Result<PetDto>.NotFound($"Pet with id {id} not found")
            : Result<PetDto>.Success(ToPetDto(pet));
    }

    public async Task<Result<List<PetDto>>> GetPetsByCustomerIdAsync(long id, CancellationToken ct)
    {
        var pets = (await petServices.GetAllByIdAsync(id, ct)).Value;
        return pets is null
            ? Result<List<PetDto>>.NotFound($"Customer with id {id} not found")
            : Result<List<PetDto>>.Success(pets.Select(ToPetDto).ToList());
    }

    private static PetDto ToPetDto(PetResponse pet) => new()
    {
        Id = pet.Id,
        CustomerId = pet.CustomersId,
        Name = pet.Name,
        Species = pet.Species.ToString(),
        Weight = (double?)pet.Weight,
        Height = (double?)pet.Height,
        Gender = pet.Gender,
        DateOfBirth = pet.DateOfBirth,
        Status = pet.Status,
        SpecialNote = pet.SpecialNote
    };
}
