using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    public class StaffShiftConfiguration : IEntityTypeConfiguration<StaffShift>
    {
        public void Configure(EntityTypeBuilder<StaffShift> Builder)
        {
            Builder.ToTable("staff_shift");

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.StaffId)
                .HasColumnName("staff_id");

            Builder.HasOne(x => x.Staff)
                .WithMany(y => y.StaffShifts)
                .HasForeignKey(z => z.StaffId)
                .HasConstraintName("fk_staff_shift_staff")
                .OnDelete(DeleteBehavior.Cascade);

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
                .HasMaxLength(15);

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");


        }
    }
}
