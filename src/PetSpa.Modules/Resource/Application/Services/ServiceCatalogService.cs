using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Services;

public class ServiceCatalogService(IServiceRepository repository, ICategoryRepository categories)
{
    private static ServiceDto ToDto(Service service)
        => new(service.Id, service.CategoryId, service.ServiceName, service.Description, service.Status);

    public async Task<ResourceResult<IReadOnlyList<ServiceDto>>> ListAsync(
        long? categoryId, string? keyword, CancellationToken ct)
    {
        if (categoryId.HasValue && await categories.GetByIdAsync(categoryId.Value, ct) is null)
            return ResourceResult<IReadOnlyList<ServiceDto>>.NotFound("Không tìm thấy danh mục.");

        var services = await repository.ListAsync(categoryId, keyword, ct);
        return ResourceResult<IReadOnlyList<ServiceDto>>.Success(services.Select(ToDto).ToList());
    }

    public async Task<ResourceResult<ServiceDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var service = await repository.GetByIdAsync(id, ct);
        return service is null
            ? ResourceResult<ServiceDto>.NotFound("Không tìm thấy dịch vụ.")
            : ResourceResult<ServiceDto>.Success(ToDto(service));
    }

    public async Task<ResourceResult<ServiceDto>> CreateAsync(
        long categoryId, string serviceName, string? description, ResourceStatus status, CancellationToken ct)
    {
        var errors = ResourceValidation.NameAndStatus(serviceName, "ServiceName", 256, status);
        ResourceValidation.ParentId(categoryId, "CategoryId", errors);
        if (errors.Count > 0)
            return ResourceResult<ServiceDto>.Invalid(errors);
        if (await categories.GetByIdAsync(categoryId, ct) is null)
            return ResourceResult<ServiceDto>.NotFound("Không tìm thấy danh mục.");

        var service = new Service
        {
            CategoryId = categoryId, ServiceName = serviceName.Trim(), Description = description, Status = status
        };
        repository.Add(service);
        await repository.SaveChangesAsync(ct);
        return ResourceResult<ServiceDto>.Success(ToDto(service));
    }

    public async Task<ResourceResult<ServiceDto>> UpdateAsync(
        long id, long categoryId, string serviceName, string? description, ResourceStatus status, CancellationToken ct)
    {
        var errors = ResourceValidation.NameAndStatus(serviceName, "ServiceName", 256, status);
        ResourceValidation.ParentId(categoryId, "CategoryId", errors);
        if (errors.Count > 0)
            return ResourceResult<ServiceDto>.Invalid(errors);

        var service = await repository.GetByIdAsync(id, ct);
        if (service is null)
            return ResourceResult<ServiceDto>.NotFound("Không tìm thấy dịch vụ.");
        if (await categories.GetByIdAsync(categoryId, ct) is null)
            return ResourceResult<ServiceDto>.NotFound("Không tìm thấy danh mục.");

        service.CategoryId = categoryId;
        service.ServiceName = serviceName.Trim();
        service.Description = description;
        service.Status = status;
        service.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync(ct);
        return ResourceResult<ServiceDto>.Success(ToDto(service));
    }

    public async Task<ResourceResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var service = await repository.GetByIdAsync(id, ct);
        if (service is null)
            return ResourceResult<bool>.NotFound("Không tìm thấy dịch vụ.");

        service.Status = ResourceStatus.DELETED;
        service.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync(ct);
        return ResourceResult<bool>.Success(true);
    }
}
