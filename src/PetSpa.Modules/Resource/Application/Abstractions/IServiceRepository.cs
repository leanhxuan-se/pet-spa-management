// Định nghĩa thao tác lưu trữ và truy vấn IServiceRepository.
using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Application.Abstractions;

public interface IServiceRepository
{
    // Lấy danh sách đối tượng.
    Task<IReadOnlyList<Service>> ListAsync(
        long? categoryId, string? keyword, CancellationToken ct, bool activeOnly = false);
    // Tìm đối tượng theo mã.
    Task<Service?> GetByIdAsync(long id, CancellationToken ct);
    // Kiểm tra tên đã tồn tại.
    Task<bool> NameExistsAsync(long categoryId, string name, long? exceptId, CancellationToken ct);
    // Thêm đối tượng mới.
    void Add(Service service);
    // Thử lưu thay đổi vào cơ sở dữ liệu.
    Task<bool> TrySaveChangesAsync(CancellationToken ct);
    // Lưu thay đổi vào cơ sở dữ liệu.
    Task SaveChangesAsync(CancellationToken ct);
}
