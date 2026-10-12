using Mapster;
using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Contracts.Dtos;
using PetSpa.SharedKernel.Application.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace PetSpa.Modules.Customer.Contracts
{
    public class CustomerContract: ICustomerContract
    {
        public readonly ICustomerService _customerService;
        public readonly IPetServices _petServices;

        public CustomerContract(
        ICustomerService customerService,
        IPetServices petService)
        {
            _customerService = customerService;
            _petServices = petService;
        }

        public async Task<Result<CustomerDto>> GetCustomerByIdAsync(long id, CancellationToken ct)
        {
            var customer = await _customerService.GetByIdAsync(id, ct);

            var customerDto = customer.Adapt<CustomerDto>();

            return Result<CustomerDto>.Success(customerDto);
        }
        public async Task<Result<PetDto>> GetPetByIdAsync(long id, CancellationToken ct)
        {
            var pet = await _petServices.GetByIdAsync(id, ct);

            var petDto = pet.Adapt<PetDto>();

            return Result<PetDto>.Success(petDto);
        }

        public async Task<Result<List<PetDto>>> GetPetsByCustomerIdAsync(long id, CancellationToken ct)
        {
            var pets = await _petServices.GetAllByIdAsync(id, ct);

            var petDtos = pets.Adapt<List<PetDto>>();

            return Result<List<PetDto>>.Success(petDtos);
        }
    }
}
