using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence;

public class OperationDbContext: DbContext
{
    public OperationDbContext(DbContextOptions<OperationDbContext> options) : base(options)
    {
    }
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<ServiceSession> ServiceSessions => Set<ServiceSession>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<StaffAssignment> StaffAssignments=> Set<StaffAssignment>();
    public DbSet<StaffShift> StaffShifts => Set<StaffShift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        //Add all table to Operation schema to prevent table name conflicts with other modules
        modelBuilder.HasDefaultSchema("operation");
        //Apply all column configurations in the future 
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OperationDbContext).Assembly);
    }
}
