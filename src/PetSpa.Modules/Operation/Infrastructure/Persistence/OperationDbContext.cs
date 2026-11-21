// Quản lý truy cập cơ sở dữ liệu qua OperationDbContext.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    internal async Task<IDbContextTransaction> BeginScheduleWriteAsync(CancellationToken ct)
    {
        const long scheduleLockKey = 557032943960932458L;
        var transaction = await Database.BeginTransactionAsync(ct);
        try
        {
            // Giữ việc kiểm tra phòng, phiên và phân công nhất quán giữa các yêu cầu ghi đồng thời.
            await Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({scheduleLockKey})", ct);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

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
