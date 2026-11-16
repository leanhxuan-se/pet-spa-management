// Quản lý truy cập cơ sở dữ liệu qua CustomerDbContext.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Domain.Entities;

using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Infrastructure.Persistence;

internal class CustomerDbContext : DbContext
{
    // Khởi tạo DbContext với cấu hình được cung cấp.
    public CustomerDbContext(DbContextOptions<CustomerDbContext> options) : base(options)
    {
    }
    public DbSet<CustomerTable> Customers => Set<CustomerTable>();
    public DbSet<Pet> Pets => Set<Pet>();
    // Cấu hình mô hình dữ liệu của DbContext.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to customer schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("customer");
        //Apply all column configurations in the future 
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CustomerDbContext).Assembly);
    }
}
