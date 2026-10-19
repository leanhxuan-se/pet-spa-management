using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.Modules.Resource.Application.Validators;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Services;

public class CategoryService(ICategoryRepository repository)
{
    private static CategoryDto ToDto(Category category)
        => new(category.Id, category.Name, category.Description, category.Status);

    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CancellationToken ct)
        => (await repository.ListAsync(ct)).Select(ToDto).ToList();

    public async Task<ResourceResult<CategoryDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(id, ct);
        return category is null 
        ? ResourceResult<CategoryDto>.NotFound("Không tìm thấy danh mục.")
        : ResourceResult<CategoryDto>.Success(ToDto(category));
    }

    public async Task<ResourceResult<CategoryDto>> CreateAsync(
        string name, string? description, ResourceStatus status, CancellationToken ct)
    {
        var errors = ResourceValidation.NameAndStatus(name, "Name", 100, status);
        if (errors.Count > 0)
            return ResourceResult<CategoryDto>.Invalid(errors);
        if (await repository.NameExistsAsync(name.Trim(), null, ct))
            return ResourceResult<CategoryDto>.Conflict("Tên danh mục đã tồn tại.");

        var category = new Category { Name = name.Trim(), Description = description, Status = status };
        repository.Add(category);
        if (!await repository.TrySaveChangesAsync(ct))
            return ResourceResult<CategoryDto>.Conflict("Tên danh mục đã tồn tại.");

        return ResourceResult<CategoryDto>.Success(ToDto(category));
    }

    public async Task<ResourceResult<CategoryDto>> UpdateAsync(
        long id, string name, string? description, ResourceStatus status, CancellationToken ct)
    {
        var errors = ResourceValidation.NameAndStatus(name, "Name", 100, status);
        if (errors.Count > 0)
            return ResourceResult<CategoryDto>.Invalid(errors);

        var category = await repository.GetByIdAsync(id, ct);
        if (category is null)
            return ResourceResult<CategoryDto>.NotFound("Không tìm thấy danh mục.");
        if (await repository.NameExistsAsync(name.Trim(), id, ct))
            return ResourceResult<CategoryDto>.Conflict("Tên danh mục đã tồn tại.");

        category.Name = name.Trim();
        category.Description = description;
        category.Status = status;
        category.UpdatedAt = DateTime.UtcNow;
        if (!await repository.TrySaveChangesAsync(ct))
            return ResourceResult<CategoryDto>.Conflict("Tên danh mục đã tồn tại.");

        return ResourceResult<CategoryDto>.Success(ToDto(category));
    }

    public async Task<ResourceResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var category = await repository.GetByIdAsync(id, ct);
        if (category is null)
            return ResourceResult<bool>.NotFound("Không tìm thấy danh mục.");

        category.Status = ResourceStatus.INACTIVE;
        category.UpdatedAt = DateTime.UtcNow;
        if (!await repository.TrySaveChangesAsync(ct))
            return ResourceResult<bool>.Conflict("Không thể lưu thay đổi danh mục.");
        return ResourceResult<bool>.Success(true);
    }
}
