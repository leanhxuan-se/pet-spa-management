// Xử lý nghiệp vụ và cung cấp dịch vụ CategoryService.
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.Modules.Resource.Application.Validators;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Services;

public class CategoryService(ICategoryRepository repository)
{
    // Chuyển đối tượng sang DTO.
    private static CategoryDto ToDto(Category category)
        => new(category.Id, category.Name, category.Description, category.Status);

    // Lấy danh sách đối tượng.
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct)
        => (await repository.ListAsync(ct)).Select(ToDto).ToList();

    // Tìm đối tượng theo mã.
    public async Task<ResourceResult<CategoryDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(id, ct);
        return category is null 
        ? ResourceResult<CategoryDto>.NotFound("Không tìm thấy danh mục.")
        : ResourceResult<CategoryDto>.Success(ToDto(category));
    }

    // Tạo đối tượng mới.
    public async Task<ResourceResult<CategoryDto>> CreateAsync(
        string name, string? description, ResourceStatus status, CancellationToken ct)
    {
        // 1. Kiểm tra dữ liệu đầu vào.
        var errors = ResourceValidation.NameAndStatus(name, "Name", 100, status);
        if (errors.Count > 0)
            return ResourceResult<CategoryDto>.Invalid(errors);
        // 2. Kiểm tra tên đã tồn tại.
        if (await repository.NameExistsAsync(name.Trim(), null, ct))
            return ResourceResult<CategoryDto>.Conflict("Tên danh mục đã tồn tại.");

        // 3. Tạo thông tin danh mục mới.
        var category = new Category { Name = name.Trim(), Description = description, Status = status };
        // 4. Thêm dữ liệu mới để lưu.
        repository.Add(category);
        // 5. Lưu thay đổi và xử lý trường hợp không thể lưu.
        if (!await repository.TrySaveChangesAsync(ct))
            return ResourceResult<CategoryDto>.Conflict("Tên danh mục đã tồn tại.");

        // 6. Trả kết quả xử lý.
        return ResourceResult<CategoryDto>.Success(ToDto(category));
    }

    // Cập nhật đối tượng.
    public async Task<ResourceResult<CategoryDto>> UpdateAsync(
        long id, string name, string? description, ResourceStatus status, CancellationToken ct)
    {
        // 1. Kiểm tra dữ liệu đầu vào.
        var errors = ResourceValidation.NameAndStatus(name, "Name", 100, status);
        if (errors.Count > 0)
            return ResourceResult<CategoryDto>.Invalid(errors);

        // 2. Lấy thông tin danh mục.
        var category = await repository.GetByIdAsync(id, ct);
        if (category is null)
            return ResourceResult<CategoryDto>.NotFound("Không tìm thấy danh mục.");
        // 3. Kiểm tra tên đã tồn tại.
        if (await repository.NameExistsAsync(name.Trim(), id, ct))
            return ResourceResult<CategoryDto>.Conflict("Tên danh mục đã tồn tại.");

        // 4. Cập nhật thông tin danh mục.
        category.Name = name.Trim();
        category.Description = description;
        category.Status = status;
        category.UpdatedAt = DateTime.UtcNow;
        // 5. Lưu thay đổi và xử lý trường hợp không thể lưu.
        if (!await repository.TrySaveChangesAsync(ct))
            return ResourceResult<CategoryDto>.Conflict("Tên danh mục đã tồn tại.");

        // 6. Trả kết quả xử lý.
        return ResourceResult<CategoryDto>.Success(ToDto(category));
    }

    // Xóa đối tượng.
    public async Task<ResourceResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        // 1. Lấy thông tin danh mục.
        var category = await repository.GetByIdAsync(id, ct);
        if (category is null)
            return ResourceResult<bool>.NotFound("Không tìm thấy danh mục.");

        // 2. Chuyển danh mục sang trạng thái ngừng hoạt động.
        category.Status = ResourceStatus.INACTIVE;
        category.UpdatedAt = DateTime.UtcNow;
        // 3. Lưu thay đổi và xử lý trường hợp không thể lưu.
        if (!await repository.TrySaveChangesAsync(ct))
            return ResourceResult<bool>.Conflict("Không thể lưu thay đổi danh mục.");
        // 4. Trả kết quả xử lý.
        return ResourceResult<bool>.Success(true);
    }
}
