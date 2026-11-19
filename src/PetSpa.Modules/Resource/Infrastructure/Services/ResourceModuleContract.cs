// Xử lý nghiệp vụ và cung cấp dịch vụ ResourceModuleContract.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Resource.Contracts;
using PetSpa.Modules.Resource.Infrastructure.Persistence;

namespace PetSpa.Modules.Resource.Infrastructure.Services;

public sealed class ResourceModuleContract(ResourceDbContext db) : IResourceModule
{
    // Lấy các tùy chọn dịch vụ đang hoạt động.
    public async Task<IReadOnlyList<BookableServiceOption>> GetActiveServiceOptionsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct)
        => await db.BookableServiceOptions.AsNoTracking()
            .Where(option => ids.Contains(option.Id))
            .Select(option => new BookableServiceOption(option.Id, option.UnitPrice)
            {
                DurationMinutes = option.DurationMinutes,
                Species = option.Species.ToString(),
                MinWeight = option.MinWeight,
                MaxWeight = option.MaxWeight
            })
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceOptionDescription>> GetServiceOptionDescriptionsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct)
        => await db.ServiceOptions.AsNoTracking()
            .Where(option => ids.Contains(option.Id))
            .Select(option => new ServiceOptionDescription(
                option.Id, option.Service.ServiceName, option.Service.Description, option.Species.ToString(),
                option.MinWeight, option.MaxWeight, option.DurationMinutes))
            .ToListAsync(ct);
}
