using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Application.Abstractions;

public interface IServiceRepository
{
    Task<IReadOnlyList<Service>> ListAsync(long? categoryId, string? keyword, CancellationToken ct);
    Task<Service?> GetByIdAsync(long id, CancellationToken ct);
    void Add(Service service);
    Task SaveChangesAsync(CancellationToken ct);
}
