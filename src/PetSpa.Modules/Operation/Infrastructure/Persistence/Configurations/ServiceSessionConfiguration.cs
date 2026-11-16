// Cấu hình ánh xạ cơ sở dữ liệu cho ServiceSession.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    public class ServiceSessionConfiguration: IEntityTypeConfiguration<ServiceSession>
    {
        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<ServiceSession> Builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            Builder.ToTable("service_session", table =>
            {
                table.HasCheckConstraint("ck_service_session_status", "status IN ('WAITING', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED')");
                table.HasCheckConstraint(
                    "ck_service_session_expected_time_order",
                    "expected_start_at IS NULL OR expected_finish_at IS NULL OR expected_finish_at >= expected_start_at");
                table.HasCheckConstraint(
                    "ck_service_session_actual_time_order",
                    "started_at IS NULL OR finished_at IS NULL OR finished_at >= started_at");
            });

            // 2. Cấu hình khóa chính.
            Builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            Builder.Property(x => x.BookingDetailId)
                .HasColumnName("booking_details_id");

            // 4. Cấu hình quan hệ hoặc chỉ mục.
            Builder.HasIndex(x => x.BookingDetailId).IsUnique();
            Builder.HasIndex(x => x.RoomId);
            Builder.HasIndex(x => x.Status);

            Builder.HasOne(x => x.Room)
                .WithMany(y => y.ServiceSessions)
                .HasForeignKey(z => z.RoomId)
                .HasConstraintName("fk_service_session_room")
                .OnDelete(DeleteBehavior.Restrict);

            Builder.Property(x => x.RoomId)
                .HasColumnName("room_id");

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasDefaultValue(ServiceSessionStatus.WAITING)
                .IsRequired()
                .HasMaxLength(30);

            Builder.Property(x => x.ExpectedStartAt)
                .HasColumnName("expected_start_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.ExpectedFinishAt)
                .HasColumnName("expected_finish_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.StartedAt)
                .HasColumnName("started_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.FinishedAt)
                .HasColumnName("finished_at")
                .HasColumnType("timestamp with time zone");

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
