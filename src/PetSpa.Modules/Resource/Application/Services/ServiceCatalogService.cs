// Xử lý nghiệp vụ và cung cấp dịch vụ ServiceCatalogService.
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.Modules.Resource.Application.Validators;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Services;

public class ServiceCatalogService(IServiceRepository repository, ICategoryRepository categories)
{
    // Chuyển đối tượng sang DTO.
    private static ServiceDto ToDto(Service service)
        => new(service.Id, service.CategoryId, service.ServiceName, service.Description, service.Status);

    // Lấy danh sách đối tượng.
    public async Task<ResourceResult<IReadOnlyList<ServiceDto>>> ListAsync(
        long? categoryId, string? keyword, CancellationToken ct)
    {
        if (categoryId.HasValue && await categories.GetByIdAsync(categoryId.Value, ct) is null)
            return ResourceResult<IReadOnlyList<ServiceDto>>.NotFound("Không tìm thấy danh mục.");

        // 1. Lấy thông tin dịch vụ.
        var services = await repository.ListAsync(categoryId, keyword, ct);
        // 2. Trả kết quả xử lý.
        return ResourceResult<IReadOnlyList<ServiceDto>>.Success(services.Select(ToDto).ToList());
    }

    // Tìm đối tượng theo mã.
    public async Task<ResourceResult<ServiceDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var service = await repository.GetByIdAsync(id, ct);
        return service is null
            ? ResourceResult<ServiceDto>.NotFound("Không tìm thấy dịch vụ.")
            : ResourceResult<ServiceDto>.Success(ToDto(service));
    }

    // Tạo đối tượng mới.
    public async Task<ResourceResult<ServiceDto>> CreateAsync(
        long categoryId, string serviceName, string? description, ResourceStatus status, CancellationToken ct)
    {
        // 1. Kiểm tra dữ liệu đầu vào.
        var errors = ResourceValidation.NameAndStatus(serviceName, "ServiceName", 150, status);
        ResourceValidation.ParentId(categoryId, "CategoryId", errors);
        if (errors.Count > 0)
            return ResourceResult<ServiceDto>.Invalid(errors);
        if (await categories.GetByIdAsync(categoryId, ct) is null)
            return ResourceResult<ServiceDto>.NotFound("Không tìm thấy danh mục.");
        // 2. Kiểm tra tên đã tồn tại.
        if (await repository.NameExistsAsync(categoryId, serviceName.Trim(), null, ct))
            return ResourceResult<ServiceDto>.Conflict("Tên dịch vụ đã tồn tại trong danh mục.");

        // 3. Tạo thông tin dịch vụ mới.
        var service = new Service
        {
            CategoryId = categoryId, ServiceName = serviceName.Trim(), Description = description, Status = status
        };
        // 4. Thêm dữ liệu mới để lưu.
        repository.Add(service);
        // 5. Lưu thay đổi và xử lý trường hợp không thể lưu.
        if (!await repository.TrySaveChangesAsync(ct))
            return ResourceResult<ServiceDto>.Conflict("Tên dịch vụ đã tồn tại trong danh mục.");
        // 6. Trả kết quả xử lý.
        return ResourceResult<ServiceDto>.Success(ToDto(service));
    }

    // Cập nhật đối tượng.
    public async Task<ResourceResult<ServiceDto>> UpdateAsync(
        long id, long categoryId, string serviceName, string? description, ResourceStatus status, CancellationToken ct)
    {
        // 1. Kiểm tra dữ liệu đầu vào.
        var errors = ResourceValidation.NameAndStatus(serviceName, "ServiceName", 150, status);
        ResourceValidation.ParentId(categoryId, "CategoryId", errors);
        if (errors.Count > 0)
            return ResourceResult<ServiceDto>.Invalid(errors);

        // 2. Lấy thông tin dịch vụ.
        var service = await repository.GetByIdAsync(id, ct);
        if (service is null)
            return ResourceResult<ServiceDto>.NotFound("Không tìm thấy dịch vụ.");
        if (await categories.GetByIdAsync(categoryId, ct) is null)
            return ResourceResult<ServiceDto>.NotFound("Không tìm thấy danh mục.");
        // 3. Kiểm tra tên đã tồn tại.
        if (await repository.NameExistsAsync(categoryId, serviceName.Trim(), id, ct))
            return ResourceResult<ServiceDto>.Conflict("Tên dịch vụ đã tồn tại trong danh mục.");

        // 4. Cập nhật thông tin dịch vụ.
        service.CategoryId = categoryId;
        service.ServiceName = serviceName.Trim();
        service.Description = description;
        service.Status = status;
        service.UpdatedAt = DateTime.UtcNow;
        // 5. Lưu thay đổi và xử lý trường hợp không thể lưu.
        if (!await repository.TrySaveChangesAsync(ct))
            return ResourceResult<ServiceDto>.Conflict("Tên dịch vụ đã tồn tại trong danh mục.");
        // 6. Trả kết quả xử lý.
        return ResourceResult<ServiceDto>.Success(ToDto(service));
    }

    // Xóa đối tượng.
    public async Task<ResourceResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        // 1. Lấy thông tin dịch vụ.
        var service = await repository.GetByIdAsync(id, ct);
        if (service is null)
            return ResourceResult<bool>.NotFound("Không tìm thấy dịch vụ.");

        // 2. Chuyển dịch vụ sang trạng thái ngừng hoạt động.
        service.Status = ResourceStatus.INACTIVE;
        service.UpdatedAt = DateTime.UtcNow;
        // 3. Lưu các thay đổi.
        await repository.SaveChangesAsync(ct);
        // 4. Trả kết quả xử lý.
        return ResourceResult<bool>.Success(true);
    }
}
