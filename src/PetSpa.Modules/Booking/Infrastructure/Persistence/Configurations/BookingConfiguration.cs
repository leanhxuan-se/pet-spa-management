using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using BookingEntity = PetSpa.Modules.Booking.Domain.Entities.Booking;

namespace PetSpa.Modules.Booking.Infrastructure.Persistence.Configurations
{
    internal class BookingConfiguration: IEntityTypeConfiguration<BookingEntity>
    {
        public void Configure(EntityTypeBuilder<BookingEntity> Builder)
        {
            Builder.ToTable("booking");

            Builder.HasKey(x => x.Id);

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

            Builder.Property(x => x.Note)
                .HasColumnName("note")
                .HasColumnType("text");

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(20);

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

        }
    }
}
