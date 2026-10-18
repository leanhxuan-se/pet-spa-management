using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Resource.Contracts;
using PetSpa.Modules.Resource.Domain.Enums;
using PetSpa.Modules.Resource.Infrastructure.Persistence;

namespace PetSpa.Modules.Resource.Infrastructure.Services;

public sealed class ResourceModuleContract(ResourceDbContext db) : IResourceModule
{
    public async Task<IReadOnlyList<BookableServiceOption>> GetActiveServiceOptionsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct)
        => await db.ServiceOptions.AsNoTracking()
            .Where(o => ids.Contains(o.Id) && o.Status == ResourceStatus.ACTIVE
                && o.Service.Status == ResourceStatus.ACTIVE
                && o.Service.Category.Status == ResourceStatus.ACTIVE)
            .Select(o => new BookableServiceOption(o.Id, o.UnitPrice))
            .ToListAsync(ct);
}
