using Microsoft.EntityFrameworkCore;

namespace PetSpa.Modules.Billing.Infrastructure.Persistence;

public class BillingDbContext : DbContext
{
  public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to billing schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("billing");
        //Apply all column configurations in the future 
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
