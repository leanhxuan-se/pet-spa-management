using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Infrastructure.Persistence;

namespace PetSpa.Modules.Resource.Infrastructure.Repositories;

public class ServiceOptionRepository(ResourceDbContext db) : IServiceOptionRepository
{
    public async Task<IReadOnlyList<ServiceOption>> ListAsync(long? serviceId, CancellationToken ct)
    {
        var query = db.ServiceOptions.AsNoTracking();
        if (serviceId.HasValue)
            query = query.Where(o => o.ServiceId == serviceId.Value);

        return await query.OrderBy(o => o.Species).ThenBy(o => o.MinWeight).ThenBy(o => o.Id).ToListAsync(ct);
    }

    public Task<ServiceOption?> GetByIdAsync(long id, CancellationToken ct)
        => db.ServiceOptions.FirstOrDefaultAsync(o => o.Id == id, ct);

    public void Add(ServiceOption option) => db.ServiceOptions.Add(option);

    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
