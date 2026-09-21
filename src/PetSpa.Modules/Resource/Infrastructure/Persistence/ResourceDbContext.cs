using Microsoft.EntityFrameworkCore;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence;

public class ResourceDbContext: DbContext
{
    public ResourceDbContext(DbContextOptions<ResourceDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to resource schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("resource");
        //Apply all column configurations in the future 
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourceDbContext).Assembly);
    }
}
