// Quản lý truy cập cơ sở dữ liệu qua OperationDbContext.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence;

internal class OperationDbContext: DbContext
{
    // Khởi tạo DbContext với cấu hình được cung cấp.
    public OperationDbContext(DbContextOptions<OperationDbContext> options) : base(options)
    {
    }
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<ServiceSession> ServiceSessions => Set<ServiceSession>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<StaffAssignment> StaffAssignments=> Set<StaffAssignment>();
    public DbSet<StaffNotification> StaffNotifications => Set<StaffNotification>();
    public DbSet<StaffShift> StaffShifts => Set<StaffShift>();

    // Cấu hình mô hình dữ liệu của DbContext.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to Operation schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("operation");
        //Apply all column configurations in the future 
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationDbContext).Assembly);
    }
}
