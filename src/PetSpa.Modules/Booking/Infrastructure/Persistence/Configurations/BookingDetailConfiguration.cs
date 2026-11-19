// Cấu hình ánh xạ cơ sở dữ liệu cho BookingDetail.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Infrastructure.Persistence.Configurations
{
    public class BookingDetailConfiguration: IEntityTypeConfiguration<BookingDetail>
    {
        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<BookingDetail> Builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            Builder.ToTable("booking_details", table =>
            {
                table.HasCheckConstraint("ck_booking_details_status",
                    "status IN ('ASSIGNED', 'WORKING', 'COMPLETED', 'CANCELLED')");
                table.HasCheckConstraint("ck_booking_details_unit_price", "unit_price >= 0");
            });

            // 2. Cấu hình khóa chính.
            Builder.HasKey(x => x.Id);
            Builder.HasIndex(x => new { x.Id, x.BookingId }).IsUnique();

            // 3. Cấu hình các cột và kiểu dữ liệu.
            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .UseIdentityByDefaultColumn();

            

            // create 1 : N relationship Booking : BookingDetail
            Builder.HasOne(x => x.Booking)
                .WithMany(y => y.BookingDetails)
                .HasForeignKey(z => z.BookingId)
                .HasConstraintName("fk_booking_detail_booking") 
                .OnDelete(DeleteBehavior.Restrict);

            Builder.Property(x => x.BookingId)
                .HasColumnName("booking_id");

            Builder.Property(x => x.ServiceOptionsId)
                .HasColumnName("service_options_id");

            Builder.Property(x => x.UnitPrice)
                .HasColumnName("unit_price")
                .HasPrecision(18, 2);

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(BookingDetailStatus.ASSIGNED)
                .IsRequired();
        }
    }
}
