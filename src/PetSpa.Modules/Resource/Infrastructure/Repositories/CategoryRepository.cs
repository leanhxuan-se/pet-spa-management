using Microsoft.EntityFrameworkCore;
using Npgsql;
using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Infrastructure.Persistence;

namespace PetSpa.Modules.Resource.Infrastructure.Repositories;

public class CategoryRepository(ResourceDbContext db) : ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct)
        => await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name).ThenBy(c => c.Id).ToListAsync(ct);

    public Task<Category?> GetByIdAsync(long id, CancellationToken ct)
        => db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, long? exceptId, CancellationToken ct)
        => db.Categories.AnyAsync(c => c.Name == name && (!exceptId.HasValue || c.Id != exceptId.Value), ct);

    public void Add(Category category) => db.Categories.Add(category);

    public async Task<bool> TrySaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_category_name" })
        {
            return false;
        }
    }
}
