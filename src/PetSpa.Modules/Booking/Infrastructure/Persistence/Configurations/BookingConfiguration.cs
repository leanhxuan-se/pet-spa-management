// Cấu hình ánh xạ cơ sở dữ liệu cho Booking.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Booking.Domain.Enums;

using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Infrastructure.Persistence.Configurations
{
    internal class BookingConfiguration: IEntityTypeConfiguration<BookingEntity>
    {
        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<BookingEntity> Builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            Builder.ToTable("booking", table =>
            {
                table.HasCheckConstraint("ck_booking_status",
                    "status IN ('PENDING', 'CONFIRMED', 'COMPLETED', 'PAID', 'CANCELLED')");
                table.HasCheckConstraint("ck_booking_check_out_order",
                    "check_out IS NULL OR check_in IS NULL OR check_out >= check_in");
                table.HasCheckConstraint("ck_booking_handoff",
                    "(sessions_created_at IS NULL AND handed_off_detail_ids IS NULL) OR "
                    + "(sessions_created_at IS NOT NULL AND handed_off_detail_ids IS NOT NULL "
                    + "AND cardinality(handed_off_detail_ids) > 0)");
            });

            // 2. Cấu hình khóa chính.
            Builder.HasKey(x => x.Id);
            Builder.HasIndex(x => x.CustomerId);
            Builder.HasIndex(x => x.PetId);
            Builder.HasIndex(x => x.BookingTime);
            Builder.HasIndex(x => x.ContactPhone);
            Builder.HasIndex(x => x.Status);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();
            
            Builder.Property(x => x.CustomerId)
                .HasColumnName("customer_id")
                .IsRequired();

            Builder.Property(x => x.PetId)
                .HasColumnName("pet_id")
                .IsRequired();

            Builder.Property(x => x.BookingTime)
                .HasColumnName("booking_time")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.CheckIn)
                .HasColumnName("check_in")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.CheckOut)
                .HasColumnName("check_out")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.ContactName)
                .HasColumnName("contact_name")
                .HasMaxLength(150);

            Builder.Property(x => x.ContactPhone)
                .HasColumnName("contact_phone")
                .HasMaxLength(30);

            Builder.Property(x => x.SessionsCreatedAt)
                .HasColumnName("sessions_created_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.HandedOffDetailIds)
                .HasColumnName("handed_off_detail_ids")
                .HasColumnType("bigint[]");

            Builder.Property(x => x.CancelledAt)
                .HasColumnName("cancelled_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.LegacyHandoffNeedsReview)
                .HasColumnName("legacy_handoff_needs_review")
                .HasDefaultValue(false);

            Builder.Property(x => x.Note)
                .HasColumnName("note")
                .HasColumnType("text");

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(BookingStatus.PENDING)
                .IsRequired();

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
