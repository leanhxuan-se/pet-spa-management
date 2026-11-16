// Định nghĩa hợp đồng hoặc kết quả dùng chung: ICustomerService.
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Customer.Application.Abstractions
{
    public interface ICustomerService
    {
        // Tìm đối tượng theo mã.
        Task<Result<CustomerResponse>> GetByIdAsync(
            long id,
            CancellationToken ct);

        // Tạo đối tượng mới.
        Task<Result<CustomerResponse>> CreateAsync(
            CreateCustomerRequest req,
            CancellationToken ct);

        // Cập nhật đối tượng.
        Task<Result<CustomerResponse>> UpdateAsync(
            long id,
            UpdateCustomerRequest req,
            CancellationToken ct);
    }
}