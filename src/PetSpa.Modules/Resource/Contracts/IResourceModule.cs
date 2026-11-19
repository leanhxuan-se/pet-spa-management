// Định nghĩa giao tiếp giữa các module: IResourceModule.
namespace PetSpa.Modules.Resource.Contracts;

public interface IResourceModule
{
    // Only options whose entire catalog hierarchy is ACTIVE are bookable.
    Task<IReadOnlyList<BookableServiceOption>> GetActiveServiceOptionsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct);

    // Includes inactive options referenced by existing bookings.
    Task<IReadOnlyList<ServiceOptionDescription>> GetServiceOptionDescriptionsAsync(
        IReadOnlyCollection<long> ids, CancellationToken ct);
}

public sealed record BookableServiceOption(long Id, decimal UnitPrice)
{
    public int DurationMinutes { get; init; }
    public string? Species { get; init; }
    public decimal? MinWeight { get; init; }
    public decimal? MaxWeight { get; init; }
}

public sealed record ServiceOptionDescription(
    long Id,
    string ServiceName,
    string? Description,
    string Species,
    decimal? MinWeight,
    decimal? MaxWeight,
    int DurationMinutes);
