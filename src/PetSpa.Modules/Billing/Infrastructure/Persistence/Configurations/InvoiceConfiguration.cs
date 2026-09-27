using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Billing.Domain.Entities;

namespace PetSpa.Modules.Billing.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration
    : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        // billing.invoice
        builder.ToTable("invoice", "billing");

        // Primary Key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.BookingId)
            .HasColumnName("booking_id")
            .IsRequired();

        builder.Property(x => x.Subtotal)
            .HasColumnName("subtotal")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasColumnName("discount_amount")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasColumnName("total_amount")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasColumnType("character varying")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.FinishedAt)
            .HasColumnName("finished_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Invoice 1 - N InvoiceItem
        builder.HasMany(x => x.Items)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // Invoice 1 - N Payment
        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
