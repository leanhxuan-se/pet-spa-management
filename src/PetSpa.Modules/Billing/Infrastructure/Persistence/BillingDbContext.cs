// Quản lý truy cập cơ sở dữ liệu qua BillingDbContext.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Billing.Domain.Entities;

namespace PetSpa.Modules.Billing.Infrastructure.Persistence;

public class BillingDbContext : DbContext
{
    // Khởi tạo DbContext với cấu hình được cung cấp.
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options)
    {
    }
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    // Cấu hình mô hình dữ liệu của DbContext.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to billing schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("billing");
        // Apply all Billing entity configurations.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BillingDbContext).Assembly);
    }
}
