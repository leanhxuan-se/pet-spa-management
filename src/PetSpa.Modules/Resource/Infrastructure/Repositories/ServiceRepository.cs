using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;
using PetSpa.Modules.Resource.Infrastructure.Persistence;

namespace PetSpa.Modules.Resource.Infrastructure.Repositories;

public class ServiceRepository(ResourceDbContext db) : IServiceRepository
{
    private IQueryable<Service> VisibleServices => db.Services.Where(s =>
        s.Status != ResourceStatus.DELETED && s.Category.Status != ResourceStatus.DELETED);

    public async Task<IReadOnlyList<Service>> ListAsync(long? categoryId, string? keyword, CancellationToken ct)
    {
        var query = VisibleServices.AsNoTracking();
        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            // Treat user input as literal text rather than PostgreSQL LIKE wildcards.
            var escaped = keyword.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            query = query.Where(s => EF.Functions.ILike(s.ServiceName, $"%{escaped}%", "\\"));
        }

        return await query.OrderBy(s => s.ServiceName).ThenBy(s => s.Id).ToListAsync(ct);
    }

    public Task<Service?> GetByIdAsync(long id, CancellationToken ct)
        => VisibleServices.FirstOrDefaultAsync(s => s.Id == id, ct);

    public void Add(Service service) => db.Services.Add(service);

    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
