// Định nghĩa thao tác lưu trữ và truy vấn IServiceOptionRepository.
using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Application.Abstractions;

public interface IServiceOptionRepository
{
    // Lấy danh sách đối tượng.
    Task<IReadOnlyList<ServiceOption>> ListAsync(long? serviceId, CancellationToken ct);
    // Tìm đối tượng theo mã.
    Task<ServiceOption?> GetByIdAsync(long id, CancellationToken ct);
    // Thêm đối tượng mới.
    void Add(ServiceOption option);
    // Lưu thay đổi vào cơ sở dữ liệu.
    Task SaveChangesAsync(CancellationToken ct);
}
