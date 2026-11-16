// Định nghĩa giao tiếp giữa các module: CustomerContract.
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


        // Lấy thông tin khách hàng theo mã.
        public async Task<Result<CustomerDto>> GetCustomerByIdAsync(long id, CancellationToken ct)
        {
            // 1. Lấy thông tin khách hàng.
            var customer = await customerService.GetByIdAsync(id, ct);

            // 2. Chuyển dữ liệu sang dạng trả về.
            var customerDto = customer.Adapt<CustomerDto>();

            // 3. Trả kết quả xử lý.
            return Result<CustomerDto>.Success(customerDto);
        }
        // Lấy thông tin thú cưng theo mã.
        public async Task<Result<PetDto>> GetPetByIdAsync(long id, CancellationToken ct)
        {
            // 1. Lấy thông tin thú cưng.
            var pet = await petServices.GetByIdAsync(id, ct);

            // 2. Chuyển dữ liệu sang dạng trả về.
            var petDto = pet.Adapt<PetDto>();

            // 3. Trả kết quả xử lý.
            return Result<PetDto>.Success(petDto);
        }

        // Lấy thú cưng theo mã khách hàng.
        public async Task<Result<List<PetDto>>> GetPetsByCustomerIdAsync(long id, CancellationToken ct)
        {
            // 1. Lấy thông tin thú cưng.
            var pets = await petServices.GetAllByIdAsync(id, ct);

            // 2. Chuyển dữ liệu sang dạng trả về.
            var petDtos = pets.Adapt<List<PetDto>>();

            // 3. Trả kết quả xử lý.
            return Result<List<PetDto>>.Success(petDtos);
        }
    }
}
