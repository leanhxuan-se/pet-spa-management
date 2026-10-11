using PetSpa.Modules.Resource.Application.Abstractions;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.ResourceSmokeTests;

// Only persistence is replaced: the HTTP routes, authorization, DI and application services are real.
internal sealed class ResourceTestStore
{
    public List<Category> Categories { get; } = [];
    public List<Service> Services { get; } = [];
    public List<ServiceOption> Options { get; } = [];
    public bool FailNextCategorySave { get; set; }

    public bool VisibleCategory(long id)
        => Categories.Any(c => c.Id == id && c.Status != ResourceStatus.DELETED);

    public bool VisibleService(Service service)
        => service.Status != ResourceStatus.DELETED && VisibleCategory(service.CategoryId);

    public bool VisibleOption(ServiceOption option)
        => option.Status != ResourceStatus.DELETED &&
            Services.Any(s => s.Id == option.ServiceId && VisibleService(s));
}

internal sealed class TestCategoryRepository(ResourceTestStore store) : ICategoryRepository
{
    public Task<IReadOnlyList<Category>> ListAsync(CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Category>>(store.Categories
            .Where(c => c.Status != ResourceStatus.DELETED).OrderBy(c => c.Name).ToList());

    public Task<Category?> GetByIdAsync(long id, CancellationToken ct)
        => Task.FromResult(store.Categories.SingleOrDefault(c => c.Id == id && c.Status != ResourceStatus.DELETED));

    public Task<bool> NameExistsAsync(string name, long? exceptId, CancellationToken ct)
        => Task.FromResult(store.Categories.Any(c => c.Name == name && c.Id != exceptId));

    public void Add(Category category)
    {
        category.Id = store.Categories.Count + 1;
        store.Categories.Add(category);
    }

    public Task<bool> TrySaveChangesAsync(CancellationToken ct)
    {
        if (!store.FailNextCategorySave)
            return Task.FromResult(true);

        store.FailNextCategorySave = false;
        return Task.FromResult(false);
    }
}

internal sealed class TestServiceRepository(ResourceTestStore store) : IServiceRepository
{
    public Task<IReadOnlyList<Service>> ListAsync(long? categoryId, string? keyword, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<Service>>(store.Services
            .Where(store.VisibleService)
            .Where(s => !categoryId.HasValue || s.CategoryId == categoryId)
            .Where(s => string.IsNullOrWhiteSpace(keyword) ||
                s.ServiceName.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.ServiceName).ToList());

    public Task<Service?> GetByIdAsync(long id, CancellationToken ct)
        => Task.FromResult(store.Services.SingleOrDefault(s => s.Id == id && store.VisibleService(s)));

    public void Add(Service service)
    {
        service.Id = store.Services.Count + 1;
        store.Services.Add(service);
    }

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

internal sealed class TestServiceOptionRepository(ResourceTestStore store) : IServiceOptionRepository
{
    public Task<IReadOnlyList<ServiceOption>> ListAsync(long? serviceId, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<ServiceOption>>(store.Options
            .Where(store.VisibleOption)
            .Where(o => !serviceId.HasValue || o.ServiceId == serviceId)
            .OrderBy(o => o.Species).ThenBy(o => o.MinWeight).ToList());

    public Task<ServiceOption?> GetByIdAsync(long id, CancellationToken ct)
        => Task.FromResult(store.Options.SingleOrDefault(o => o.Id == id && store.VisibleOption(o)));

    public void Add(ServiceOption option)
    {
        option.Id = store.Options.Count + 1;
        store.Options.Add(option);
    }

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}
