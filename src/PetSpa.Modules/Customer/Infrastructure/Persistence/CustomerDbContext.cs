using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Domain.Entities;

using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Infrastructure.Persistence;

public class CustomerDbContext : DbContext
{
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }
    public DbSet<CustomerTable> Customers => Set<CustomerTable>();
    public DbSet<Pet> Pets => Set<Pet>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to customer schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("customer");
        //Apply all column configurations in the future 
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerDbContext).Assembly);
    }
}
