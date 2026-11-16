// Định nghĩa thao tác lưu trữ và truy vấn ICategoryRepository.
using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Application.Abstractions;

public interface ICategoryRepository
{
    // Lấy danh sách đối tượng.
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct);
    // Tìm đối tượng theo mã.
    Task<Category?> GetByIdAsync(long id, CancellationToken ct);
    // Check all records, including deleted ones, to respect the unique database index.
    Task<bool> NameExistsAsync(string name, long? exceptId, CancellationToken ct);
    // Thêm đối tượng mới.
    void Add(Category category);
    // False means the unique category name constraint was violated.
    Task<bool> TrySaveChangesAsync(CancellationToken ct);
}
