using Mapster;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Contracts.Dtos;
using PetSpa.SharedKernel.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Customer.Contracts
{
    public class CustomerContract (
        ICustomerService customerService,
        IPetServices petServices) : ICustomerContract
    {


        public async Task<Result<CustomerDto>> GetCustomerByIdAsync(long id, CancellationToken ct)
        {
            var customer = await customerService.GetByIdAsync(id, ct);

            var customerDto = customer.Adapt<CustomerDto>();

            return Result<CustomerDto>.Success(customerDto);
        }
        public async Task<Result<PetDto>> GetPetByIdAsync(long id, CancellationToken ct)
        {
            var pet = await petServices.GetByIdAsync(id, ct);

            var petDto = pet.Adapt<PetDto>();

            return Result<PetDto>.Success(petDto);
        }

        public async Task<Result<List<PetDto>>> GetPetsByCustomerIdAsync(long id, CancellationToken ct)
        {
            var pets = await petServices.GetAllByIdAsync(id, ct);

            var petDtos = pets.Adapt<List<PetDto>>();

            return Result<List<PetDto>>.Success(petDtos);
        }
    }
}
