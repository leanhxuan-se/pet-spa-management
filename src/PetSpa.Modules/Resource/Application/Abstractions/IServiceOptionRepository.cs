using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Application.Abstractions;

public interface IServiceOptionRepository
{
    Task<IReadOnlyList<ServiceOption>> ListAsync(long? serviceId, CancellationToken ct);
    Task<ServiceOption?> GetByIdAsync(long id, CancellationToken ct);
    void Add(ServiceOption option);
    Task SaveChangesAsync(CancellationToken ct);
}
