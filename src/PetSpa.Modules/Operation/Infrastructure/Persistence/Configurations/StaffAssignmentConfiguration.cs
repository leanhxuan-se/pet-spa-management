// Cấu hình ánh xạ cơ sở dữ liệu cho StaffAssignment.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    internal class StaffAssignmentConfiguration : IEntityTypeConfiguration<StaffAssignment>
    {
        // Cấu hình ánh xạ thực thể phân công nhân viên vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<StaffAssignment> Builder)
        {
            // 1. Cấu hình tên bảng và ràng buộc nhất quán trạng thái hiệu lực.
            Builder.ToTable("staff_assignment", table =>
            {
                table.HasCheckConstraint(
                    "ck_staff_assignment_active_state",
                    "(is_active = TRUE AND revoked_at IS NULL) OR (is_active = FALSE AND revoked_at IS NOT NULL)");
            });

            // 2. Cấu hình khóa chính.
            Builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            // Chỉ ngăn trùng lặp giữa các phân công còn hiệu lực; giữ lại lịch sử nhân viên cũ nếu đổi người rồi giao lại.
            Builder.HasIndex(x => new { x.ServiceSessionId, x.StaffId })
                .IsUnique()
                .HasFilter("is_active = TRUE");

            // 4. Cấu hình quan hệ và chỉ mục truy vấn nhanh theo trạng thái hiệu lực.
            Builder.HasIndex(x => x.ServiceSessionId);
            Builder.HasIndex(x => x.StaffId);
            Builder.HasIndex(x => new { x.StaffId, x.IsActive });
            Builder.HasIndex(x => new { x.ServiceSessionId, x.IsActive });

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

            Builder.Property(x => x.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            Builder.Property(x => x.AssignedAt)
                .HasColumnName("assigned_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            Builder.Property(x => x.RevokedAt)
                .HasColumnName("revoked_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.ReplacedByAssignmentId)
                .HasColumnName("replaced_by_assignment_id")
                .HasColumnType("bigint");
        }
    }
}
