// Cấu hình ánh xạ cơ sở dữ liệu cho InvoiceItem.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Billing.Domain.Entities;

namespace PetSpa.Modules.Billing.Infrastructure.Persistence.Configurations;

public class InvoiceItemConfiguration
    : IEntityTypeConfiguration<InvoiceItem>
{
    // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        // 1. Cấu hình tên bảng và các ràng buộc.
        builder.ToTable("invoice_item", "billing");

        // 2. Cấu hình khóa chính.
        builder.HasKey(x => x.Id);

        // 3. Cấu hình các cột và kiểu dữ liệu.
        builder.Property(x => x.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.InvoiceId)
            .HasColumnName("invoice_id")
            .IsRequired();

        builder.Property(x => x.BookingDetailId)
            .HasColumnName("booking_details_id")
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnName("amount")
            .HasColumnType("bigint")
            .IsRequired();

        // 4. Cấu hình quan hệ hoặc chỉ mục.
        builder.HasIndex(x => x.InvoiceId);
    }
}
