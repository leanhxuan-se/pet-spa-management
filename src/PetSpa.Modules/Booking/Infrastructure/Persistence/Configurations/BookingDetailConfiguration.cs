using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Booking.Domain.Entities;

namespace PetSpa.Modules.Booking.Infrastructure.Persistence.Configurations
{
    public class BookingDetailConfiguration: IEntityTypeConfiguration<BookingDetail>
    {
        public void Configure(EntityTypeBuilder<BookingDetail> Builder)
        {
            Builder.ToTable("booking_details");

            Builder.HasKey(x => x.Id);

            // create 1 : N relationship Booking : BookingDetail
            Builder.HasOne(x => x.Booking)
                .WithMany(y => y.BookingDetails)
                .HasForeignKey(z => z.BookingId)
                .HasConstraintName("fk_booking_detail_booking") 
                .OnDelete(DeleteBehavior.Cascade); // auto delete BookingDetail when Booking is deleted 

            Builder.Property(x => x.BookingId)
                .HasColumnName("booking_id");

            Builder.Property(x => x.ServiceOptionsId)
                .HasColumnName("service_options_id");

            Builder.Property(x => x.UnitPrice)
                .HasColumnName("unit_price");

            Builder.Property(x => x.Status)
                .HasColumnName("status");
        }
    }
}
