using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence;

public class ResourceDbContext: DbContext
{
    public ResourceDbContext(DbContextOptions<ResourceDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to resource schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("resource");
        //Apply all column configurations in the future 
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourceDbContext).Assembly);
    }
}
