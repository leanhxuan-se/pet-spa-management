using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Booking.Domain.Entities;
using PetSpa.Modules.Booking.Domain.Enums;

namespace PetSpa.Modules.Booking.Infrastructure.Persistence.Configurations
{
    public class BookingDetailConfiguration: IEntityTypeConfiguration<BookingDetail>
    {
        public void Configure(EntityTypeBuilder<BookingDetail> Builder)
        {
            Builder.ToTable("booking_details", table =>
            {
                table.HasCheckConstraint("ck_booking_details_status",
                    "status IN ('ASSIGNED', 'WORKING', 'COMPLETED', 'CANCELLED')");
                table.HasCheckConstraint("ck_booking_details_unit_price", "unit_price >= 0");
            });

            Builder.HasKey(x => x.Id);

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
