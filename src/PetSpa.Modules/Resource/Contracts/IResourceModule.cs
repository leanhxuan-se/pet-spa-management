namespace PetSpa.Modules.Resource.Contracts;

public interface IResourceModule
{
    // Only options whose entire catalog hierarchy is ACTIVE are bookable.
    Task<IReadOnlyList<BookableServiceOption>> GetActiveServiceOptionsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct);
}

public sealed record BookableServiceOption(long Id, decimal UnitPrice);
