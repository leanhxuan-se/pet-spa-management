using Microsoft.EntityFrameworkCore;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence;

public class OperationDbContext: DbContext
{
    public OperationDbContext(DbContextOptions<OperationDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to Operation schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("Operation");
        //Apply all column configurations in the future 
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationDbContext).Assembly);
    }
}
