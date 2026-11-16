// Định nghĩa hợp đồng hoặc kết quả dùng chung: IPetServices.
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.Modules.Customer.Endpoints.Responses;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Application.Abstractions
{
    public interface IPetServices
    {
        // Tìm đối tượng theo mã.
        Task<Result<PetResponse>> GetByIdAsync(
            long id, 
            CancellationToken ct);

        // Lấy danh sách theo mã liên quan.
        Task<Result<List<PetResponse>>> GetAllByIdAsync(
            long id, 
            CancellationToken ct);

        // Tạo đối tượng mới.
        Task<Result<PetResponse>> CreateAsync(
            CreatePetRequest req, 
            CancellationToken ct);

        // Cập nhật đối tượng.
        Task<Result<PetResponse>> UpdateAsync(
            long id, 
            UpdatePetRequest req, 
            CancellationToken ct);
    }
}
