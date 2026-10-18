using Microsoft.EntityFrameworkCore;
using Npgsql;
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Infrastructure.Persistence;

namespace PetSpa.Modules.Resource.Infrastructure.Repositories;

public class ServiceRepository(ResourceDbContext db) : IServiceRepository
{
    public async Task<IReadOnlyList<Service>> ListAsync(long? categoryId, string? keyword, CancellationToken ct)
    {
        var query = db.Services.AsNoTracking();
        if (categoryId.HasValue)
            query = query.Where(s => s.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var escaped = keyword.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
            query = query.Where(s => EF.Functions.ILike(s.ServiceName, $"%{escaped}%", "\\"));
        }

        return await query.OrderBy(s => s.ServiceName).ThenBy(s => s.Id).ToListAsync(ct);
    }

    public Task<Service?> GetByIdAsync(long id, CancellationToken ct)
        => db.Services.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> NameExistsAsync(long categoryId, string name, long? exceptId, CancellationToken ct)
        => db.Services.AnyAsync(s => s.CategoryId == categoryId && s.ServiceName == name &&
            (!exceptId.HasValue || s.Id != exceptId.Value), ct);

    public void Add(Service service) => db.Services.Add(service);

    public async Task<bool> TrySaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_service_category_id_service_name" })
        {
            return false;
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct) => await db.SaveChangesAsync(ct);
}
