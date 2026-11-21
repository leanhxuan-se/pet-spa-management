// Cấu hình ánh xạ cơ sở dữ liệu cho StaffNotification.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    // Ánh xạ bảng operation.staff_notification và các ràng buộc chống trùng lặp.
    internal class StaffNotificationConfiguration : IEntityTypeConfiguration<StaffNotification>
    {
        // Cấu hình bảng, cột, khóa ngoại và chỉ mục cho thông báo nội bộ.
        public void Configure(EntityTypeBuilder<StaffNotification> Builder)
        {
            // 1. Cấu hình tên bảng và check constraint cho loại thông báo, trạng thái đã đọc.
            Builder.ToTable("staff_notification", table =>
            {
                table.HasCheckConstraint(
                    "ck_staff_notification_type",
                    "type IN ('ASSIGNED', 'REASSIGNED', 'REMINDER')");
                table.HasCheckConstraint(
                    "ck_staff_notification_read_state",
                    "(is_read = FALSE AND read_at IS NULL) OR (is_read = TRUE AND read_at IS NOT NULL)");
            });

            // 2. Cấu hình khóa chính.
            Builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            Builder.Property(x => x.StaffAssignmentId)
                .HasColumnName("staff_assignment_id");

            Builder.Property(x => x.Type)
                .HasColumnName("type")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(30);

            Builder.Property(x => x.Title)
                .HasColumnName("title")
                .IsRequired()
                .HasMaxLength(200);

            Builder.Property(x => x.Message)
                .HasColumnName("message")
                .IsRequired()
                .HasMaxLength(1000);

            Builder.Property(x => x.IsRead)
                .HasColumnName("is_read")
                .HasDefaultValue(false)
                .IsRequired();

            Builder.Property(x => x.ReadAt)
                .HasColumnName("read_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.IdempotencyKey)
                .HasColumnName("idempotency_key")
                .HasMaxLength(150);

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            // 4. Cấu hình quan hệ khóa ngoại và chỉ mục truy vấn.
            Builder.HasOne(x => x.StaffAssignment)
                .WithMany()
                .HasForeignKey(x => x.StaffAssignmentId)
                .HasConstraintName("fk_staff_notification_staff_assignment")
                .OnDelete(DeleteBehavior.Restrict);

            Builder.HasIndex(x => new { x.StaffAssignmentId, x.IsRead, x.CreatedAt });
            Builder.HasIndex(x => x.IdempotencyKey)
                .IsUnique()
                .HasFilter("idempotency_key IS NOT NULL");
        }
    }
}
