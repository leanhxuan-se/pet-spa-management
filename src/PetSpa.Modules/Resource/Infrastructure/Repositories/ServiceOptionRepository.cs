// Định nghĩa thao tác lưu trữ và truy vấn ServiceOptionRepository.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;
using PetSpa.Modules.Resource.Infrastructure.Persistence;

namespace PetSpa.Modules.Resource.Infrastructure.Repositories;

public class ServiceOptionRepository(ResourceDbContext db) : IServiceOptionRepository
{
    // Lấy danh sách đối tượng.
    public async Task<IReadOnlyList<ServiceOption>> ListAsync(
        long? serviceId, CancellationToken ct, bool activeOnly = false, Species? species = null, decimal? weight = null)
    {
        var query = (activeOnly ? db.BookableServiceOptions : db.ServiceOptions).AsNoTracking();
        if (serviceId is { } selectedService)
            query = query.Where(o => o.ServiceId == selectedService);
        if (species is { } selectedSpecies)
            query = query.Where(o => o.Species == selectedSpecies);
        if (weight is { } petWeight)
            query = query.Where(o => (o.MinWeight == null || o.MinWeight <= petWeight) &&
                (o.MaxWeight == null || o.MaxWeight >= petWeight));

        return await query.OrderBy(o => o.Species).ThenBy(o => o.MinWeight).ThenBy(o => o.Id).ToListAsync(ct);
    }

    // Tìm đối tượng theo mã.
    public Task<ServiceOption?> GetByIdAsync(long id, CancellationToken ct)
        => db.ServiceOptions.FirstOrDefaultAsync(o => o.Id == id, ct);

    // Thêm đối tượng mới.
    public void Add(ServiceOption option) => db.ServiceOptions.Add(option);

    // Lưu thay đổi vào cơ sở dữ liệu.
    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
