using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    public class StaffShiftConfiguration : IEntityTypeConfiguration<StaffShift>
    {
        public void Configure(EntityTypeBuilder<StaffShift> Builder)
        {
            Builder.ToTable("staff_shift", table =>
            {
                table.HasCheckConstraint("ck_staff_shift_status", "status IN ('SCHEDULED', 'COMPLETED', 'CANCELLED')");
                table.HasCheckConstraint("ck_staff_shift_time_order", "end_time > start_time");
            });

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            Builder.HasIndex(x => x.StaffId);
            Builder.HasIndex(x => x.ShiftDate);
            Builder.HasIndex(x => new { x.StaffId, x.ShiftDate });

            Builder.Property(x => x.StaffId)
                .HasColumnName("staff_id");

            Builder.HasOne(x => x.Staff)
                .WithMany(y => y.StaffShifts)
                .HasForeignKey(z => z.StaffId)
                .HasConstraintName("fk_staff_shift_staff")
                .OnDelete(DeleteBehavior.Restrict);

            Builder.Property(x => x.ShiftDate)
                .HasColumnName("shift_date")
                .HasColumnType("date");

            Builder.Property(x => x.StartTime)
                .HasColumnName("start_time")
                .HasColumnType("time");

            Builder.Property(x => x.EndTime)
                .HasColumnName("end_time")
                .HasColumnType("time");

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasDefaultValue(StaffShiftStatus.SCHEDULED)
                .IsRequired()
                .HasMaxLength(30);

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");


        }
    }
}
