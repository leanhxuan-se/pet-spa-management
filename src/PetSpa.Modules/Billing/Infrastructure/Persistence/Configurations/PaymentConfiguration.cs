using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Billing.Domain.Entities;

namespace PetSpa.Modules.Billing.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payment", "billing");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal")
            .IsRequired();

        builder.Property(x => x.PaymentMethod)
            .HasColumnName("payment_method")
            .HasConversion<string>()
            .HasColumnType("character varying")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasColumnType("character varying")
            .IsRequired();

        builder.Property(x => x.TransactionReference)
            .HasColumnName("transaction_reference")
            .HasColumnType("character varying")
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .HasColumnName("paid_at")
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.InvoiceId);
    }
}
