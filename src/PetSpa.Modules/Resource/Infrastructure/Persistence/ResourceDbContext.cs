// Quản lý truy cập cơ sở dữ liệu qua ResourceDbContext.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence;

public class ResourceDbContext: DbContext
{
    // Khởi tạo DbContext với cấu hình được cung cấp.
    public ResourceDbContext(DbContextOptions<ResourceDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceOption> ServiceOptions => Set<ServiceOption>();
    // Cấu hình mô hình dữ liệu của DbContext.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to resource schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("resource");
        // Apply configurations owned by this module.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResourceDbContext).Assembly);
    }
}
