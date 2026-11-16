// Cấu hình ánh xạ cơ sở dữ liệu cho StaffAssignment.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    public class StaffAssignmentConfiguration : IEntityTypeConfiguration<StaffAssignment>
    {
        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<StaffAssignment> Builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            Builder.ToTable("staff_assignment");

            // 2. Cấu hình khóa chính.
            Builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            // Một nhân viên chỉ được phân công vào một lượt dịch vụ một lần
            Builder.HasIndex(x => new { x.ServiceSessionId, x.StaffId })
                .IsUnique();
            // 4. Cấu hình quan hệ hoặc chỉ mục.
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
