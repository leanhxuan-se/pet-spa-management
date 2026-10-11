using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Services;

public class ServiceOptionService(IServiceOptionRepository repository, IServiceRepository services)
{
    private static ServiceOptionDto ToDto(ServiceOption option)
        => new(option.Id, option.ServiceId, option.Species, option.MinWeight, option.MaxWeight,
            option.DurationMinutes, option.UnitPrice, option.Status);

    public async Task<ResourceResult<IReadOnlyList<ServiceOptionDto>>> ListAsync(long? serviceId, CancellationToken ct)
    {
        if (serviceId.HasValue && await services.GetByIdAsync(serviceId.Value, ct) is null)
            return ResourceResult<IReadOnlyList<ServiceOptionDto>>.NotFound("Không tìm thấy dịch vụ.");

        var options = await repository.ListAsync(serviceId, ct);
        return ResourceResult<IReadOnlyList<ServiceOptionDto>>.Success(options.Select(ToDto).ToList());
    }

    public async Task<ResourceResult<ServiceOptionDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var option = await repository.GetByIdAsync(id, ct);
        return option is null
            ? ResourceResult<ServiceOptionDto>.NotFound("Không tìm thấy tùy chọn dịch vụ.")
            : ResourceResult<ServiceOptionDto>.Success(ToDto(option));
    }

    public async Task<ResourceResult<ServiceOptionDto>> CreateAsync(
        long serviceId, Species species, decimal? minWeight, decimal? maxWeight,
        int durationMinutes, decimal unitPrice, ResourceStatus status, CancellationToken ct)
    {
        var errors = Validate(serviceId, species, minWeight, maxWeight, durationMinutes, unitPrice, status);
        if (errors.Count > 0)
            return ResourceResult<ServiceOptionDto>.Invalid(errors);
        if (await services.GetByIdAsync(serviceId, ct) is null)
            return ResourceResult<ServiceOptionDto>.NotFound("Không tìm thấy dịch vụ.");

        var option = new ServiceOption
        {
            ServiceId = serviceId, Species = species, MinWeight = minWeight, MaxWeight = maxWeight,
            DurationMinutes = durationMinutes, UnitPrice = unitPrice, Status = status
        };
        repository.Add(option);
        await repository.SaveChangesAsync(ct);
        return ResourceResult<ServiceOptionDto>.Success(ToDto(option));
    }

    public async Task<ResourceResult<ServiceOptionDto>> UpdateAsync(
        long id, long serviceId, Species species, decimal? minWeight, decimal? maxWeight,
        int durationMinutes, decimal unitPrice, ResourceStatus status, CancellationToken ct)
    {
        var errors = Validate(serviceId, species, minWeight, maxWeight, durationMinutes, unitPrice, status);
        if (errors.Count > 0)
            return ResourceResult<ServiceOptionDto>.Invalid(errors);

        var option = await repository.GetByIdAsync(id, ct);
        if (option is null)
            return ResourceResult<ServiceOptionDto>.NotFound("Không tìm thấy tùy chọn dịch vụ.");
        if (await services.GetByIdAsync(serviceId, ct) is null)
            return ResourceResult<ServiceOptionDto>.NotFound("Không tìm thấy dịch vụ.");

        option.ServiceId = serviceId;
        option.Species = species;
        option.MinWeight = minWeight;
        option.MaxWeight = maxWeight;
        option.DurationMinutes = durationMinutes;
        option.UnitPrice = unitPrice;
        option.Status = status;
        option.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync(ct);
        return ResourceResult<ServiceOptionDto>.Success(ToDto(option));
    }

    public async Task<ResourceResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        var option = await repository.GetByIdAsync(id, ct);
        if (option is null)
            return ResourceResult<bool>.NotFound("Không tìm thấy tùy chọn dịch vụ.");

        option.Status = ResourceStatus.DELETED;
        option.UpdatedAt = DateTime.UtcNow;
        await repository.SaveChangesAsync(ct);
        return ResourceResult<bool>.Success(true);
    }

    private static Dictionary<string, string[]> Validate(
        long serviceId, Species species, decimal? minWeight, decimal? maxWeight,
        int durationMinutes, decimal unitPrice, ResourceStatus status)
    {
        var errors = new Dictionary<string, string[]>();
        ResourceValidation.ParentId(serviceId, "ServiceId", errors);
        ResourceValidation.Status(status, errors);
        if (!Enum.IsDefined(species))
            errors["Species"] = ["Loài thú cưng không hợp lệ."];
        if (minWeight < 0)
            errors["MinWeight"] = ["Cân nặng tối thiểu không được âm."];
        if (maxWeight < 0)
            errors["MaxWeight"] = ["Cân nặng tối đa không được âm."];
        if (minWeight.HasValue && maxWeight.HasValue && minWeight.Value > maxWeight.Value)
            errors["MaxWeight"] = ["Cân nặng tối đa phải lớn hơn hoặc bằng cân nặng tối thiểu."];
        if (durationMinutes <= 0)
            errors["DurationMinutes"] = ["Thời lượng phải lớn hơn 0 phút."];
        if (unitPrice < 0)
            errors["UnitPrice"] = ["Đơn giá không được âm."];
        return errors;
    }
}
