using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Application.Abstractions;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct);
    Task<Category?> GetByIdAsync(long id, CancellationToken ct);
    // Check all records, including deleted ones, to respect the unique database index.
    Task<bool> NameExistsAsync(string name, long? exceptId, CancellationToken ct);
    void Add(Category category);
    // False means the unique category name constraint was violated.
    Task<bool> TrySaveChangesAsync(CancellationToken ct);
}
