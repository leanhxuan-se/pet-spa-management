using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    public class StaffAssignmentConfiguration : IEntityTypeConfiguration<StaffAssignment>
    {
        public void Configure(EntityTypeBuilder<StaffAssignment> Builder)
        {
            Builder.ToTable("staff_assignment");

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            // Một nhân viên chỉ được phân công vào một lượt dịch vụ một lần
            Builder.HasIndex(x => new { x.ServiceSessionId, x.StaffId })
                .IsUnique();
            Builder.HasIndex(x => x.ServiceSessionId);
            Builder.HasIndex(x => x.StaffId);

            Builder.Property(x => x.ServiceSessionId)
                .HasColumnName("service_session_id");

            Builder.HasOne(x => x.ServiceSession)
                .WithMany(y => y.StaffAssignments)
                .HasForeignKey(z => z.ServiceSessionId)
                .HasConstraintName("fk_staff_assignment_service_session")
                .OnDelete(DeleteBehavior.Restrict);

            Builder.Property(x => x.StaffId)
                .HasColumnName("staff_id");

            Builder.HasOne(x => x.Staff)
                .WithMany(y => y.StaffAssignments)
                .HasForeignKey(z => z.StaffId)
                .HasConstraintName("fk_staff_assignment_staff")
                .OnDelete(DeleteBehavior.Restrict);

            Builder.Property(x => x.AssignedAt)
                .HasColumnName("assigned_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");


        }
    }
}
