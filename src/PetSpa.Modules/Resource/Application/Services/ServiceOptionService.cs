// Xử lý nghiệp vụ và cung cấp dịch vụ ServiceOptionService.
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Application.Dtos;
using PetSpa.Modules.Resource.Application.Validators;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Application.Services;

public class ServiceOptionService(IServiceOptionRepository repository, IServiceRepository services)
{
    // Chuyển đối tượng sang DTO.
    private static ServiceOptionDto ToDto(ServiceOption option)
        => new(option.Id, option.ServiceId, option.Species, option.MinWeight, option.MaxWeight,
            option.DurationMinutes, option.UnitPrice, option.Status);

    // Lấy danh sách đối tượng.
    public async Task<ResourceResult<IReadOnlyList<ServiceOptionDto>>> ListAsync(
        long? serviceId, CancellationToken ct, bool activeOnly = false, Species? species = null, decimal? weight = null)
    {
        var errors = new Dictionary<string, string[]>();
        if (species is { } selectedSpecies && !Enum.IsDefined(selectedSpecies))
            errors["Species"] = ["Loài thú cưng không hợp lệ."];
        if (weight < 0)
            errors["Weight"] = ["Cân nặng không được âm."];
        if (errors.Count > 0)
            return ResourceResult<IReadOnlyList<ServiceOptionDto>>.Invalid(errors);

        if (serviceId.HasValue && await services.GetByIdAsync(serviceId.Value, ct) is null)
            return ResourceResult<IReadOnlyList<ServiceOptionDto>>.NotFound("Không tìm thấy dịch vụ.");

        // 1. Lấy thông tin tùy chọn dịch vụ.
        var options = await repository.ListAsync(serviceId, ct, activeOnly, species, weight);
        // 2. Trả kết quả xử lý.
        return ResourceResult<IReadOnlyList<ServiceOptionDto>>.Success(options.Select(ToDto).ToList());
    }

    // Tìm đối tượng theo mã.
    public async Task<ResourceResult<ServiceOptionDto>> GetByIdAsync(long id, CancellationToken ct)
    {
        var option = await repository.GetByIdAsync(id, ct);
        return option is null
            ? ResourceResult<ServiceOptionDto>.NotFound("Không tìm thấy tùy chọn dịch vụ.")
            : ResourceResult<ServiceOptionDto>.Success(ToDto(option));
    }

    // Tạo đối tượng mới.
    public async Task<ResourceResult<ServiceOptionDto>> CreateAsync(
        long serviceId, Species species, decimal? minWeight, decimal? maxWeight,
        int durationMinutes, decimal unitPrice, ResourceStatus status, CancellationToken ct)
    {
        // 1. Kiểm tra dữ liệu đầu vào.
        var errors = Validate(serviceId, species, minWeight, maxWeight, durationMinutes, unitPrice, status);
        if (errors.Count > 0)
            return ResourceResult<ServiceOptionDto>.Invalid(errors);
        if (await services.GetByIdAsync(serviceId, ct) is null)
            return ResourceResult<ServiceOptionDto>.NotFound("Không tìm thấy dịch vụ.");

        // 2. Tạo thông tin tùy chọn dịch vụ mới.
        var option = new ServiceOption
        {
            ServiceId = serviceId, Species = species, MinWeight = minWeight, MaxWeight = maxWeight,
            DurationMinutes = durationMinutes, UnitPrice = unitPrice, Status = status
        };
        // 3. Thêm dữ liệu mới để lưu.
        repository.Add(option);
        // 4. Lưu các thay đổi.
        await repository.SaveChangesAsync(ct);
        // 5. Trả kết quả xử lý.
        return ResourceResult<ServiceOptionDto>.Success(ToDto(option));
    }

    // Cập nhật đối tượng.
    public async Task<ResourceResult<ServiceOptionDto>> UpdateAsync(
        long id, long serviceId, Species species, decimal? minWeight, decimal? maxWeight,
        int durationMinutes, decimal unitPrice, ResourceStatus status, CancellationToken ct)
    {
        // 1. Kiểm tra dữ liệu đầu vào.
        var errors = Validate(serviceId, species, minWeight, maxWeight, durationMinutes, unitPrice, status);
        if (errors.Count > 0)
            return ResourceResult<ServiceOptionDto>.Invalid(errors);

        // 2. Lấy thông tin tùy chọn dịch vụ.
        var option = await repository.GetByIdAsync(id, ct);
        if (option is null)
            return ResourceResult<ServiceOptionDto>.NotFound("Không tìm thấy tùy chọn dịch vụ.");
        if (await services.GetByIdAsync(serviceId, ct) is null)
            return ResourceResult<ServiceOptionDto>.NotFound("Không tìm thấy dịch vụ.");

        // 3. Cập nhật thông tin tùy chọn dịch vụ.
        option.ServiceId = serviceId;
        option.Species = species;
        option.MinWeight = minWeight;
        option.MaxWeight = maxWeight;
        option.DurationMinutes = durationMinutes;
        option.UnitPrice = unitPrice;
        option.Status = status;
        option.UpdatedAt = DateTime.UtcNow;
        // 4. Lưu các thay đổi.
        await repository.SaveChangesAsync(ct);
        // 5. Trả kết quả xử lý.
        return ResourceResult<ServiceOptionDto>.Success(ToDto(option));
    }

    // Xóa đối tượng.
    public async Task<ResourceResult<bool>> DeleteAsync(long id, CancellationToken ct)
    {
        // 1. Lấy thông tin tùy chọn dịch vụ.
        var option = await repository.GetByIdAsync(id, ct);
        if (option is null)
            return ResourceResult<bool>.NotFound("Không tìm thấy tùy chọn dịch vụ.");

        // 2. Chuyển tùy chọn dịch vụ sang trạng thái ngừng hoạt động.
        option.Status = ResourceStatus.INACTIVE;
        option.UpdatedAt = DateTime.UtcNow;
        // 3. Lưu các thay đổi.
        await repository.SaveChangesAsync(ct);
        // 4. Trả kết quả xử lý.
        return ResourceResult<bool>.Success(true);
    }

    // Kiểm tra dữ liệu đầu vào.
    private static Dictionary<string, string[]> Validate(
        long serviceId, Species species, decimal? minWeight, decimal? maxWeight,
        int durationMinutes, decimal unitPrice, ResourceStatus status)
    {
        // 1. Kiểm tra dịch vụ, trạng thái và loài thú cưng.
        var errors = new Dictionary<string, string[]>();
        ResourceValidation.ParentId(serviceId, "ServiceId", errors);
        ResourceValidation.Status(status, errors);
        if (!Enum.IsDefined(species))
            errors["Species"] = ["Loài thú cưng không hợp lệ."];
        // 2. Kiểm tra giới hạn cân nặng.
        if (minWeight is < 0 or > 9999.99m ||
            (minWeight.HasValue && decimal.Round(minWeight.Value, 2) != minWeight.Value))
            errors["MinWeight"] = ["Cân nặng phải từ 0 đến 9999.99 kg và tối đa 2 chữ số thập phân."];
        if (maxWeight is < 0 or > 9999.99m ||
            (maxWeight.HasValue && decimal.Round(maxWeight.Value, 2) != maxWeight.Value))
            errors["MaxWeight"] = ["Cân nặng phải từ 0 đến 9999.99 kg và tối đa 2 chữ số thập phân."];
        if (minWeight.HasValue && maxWeight.HasValue && minWeight.Value > maxWeight.Value)
            errors["MaxWeight"] = ["Cân nặng tối đa phải lớn hơn hoặc bằng cân nặng tối thiểu."];
        // 3. Kiểm tra thời lượng và đơn giá.
        if (durationMinutes <= 0)
            errors["DurationMinutes"] = ["Thời lượng phải lớn hơn 0 phút."];
        if (unitPrice is < 0 or > 9999999999999999.99m || decimal.Round(unitPrice, 2) != unitPrice)
            errors["UnitPrice"] = ["Đơn giá phải từ 0 đến 9999999999999999.99 VND và tối đa 2 chữ số thập phân."];
        return errors;
    }
}
