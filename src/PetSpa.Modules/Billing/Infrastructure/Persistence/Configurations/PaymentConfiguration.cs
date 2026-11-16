// Cấu hình ánh xạ cơ sở dữ liệu cho Payment.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Billing.Domain.Entities;

namespace PetSpa.Modules.Billing.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // 1. Cấu hình tên bảng và các ràng buộc.
        builder.ToTable("payment", "billing");

        // 2. Cấu hình khóa chính.
        builder.HasKey(x => x.Id);

        // 3. Cấu hình các cột và kiểu dữ liệu.
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

        // 4. Cấu hình quan hệ hoặc chỉ mục.
        builder.HasIndex(x => x.InvoiceId);
    }
}
