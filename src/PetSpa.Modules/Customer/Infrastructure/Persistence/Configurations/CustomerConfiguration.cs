// Cấu hình ánh xạ cơ sở dữ liệu cho Customer.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Customer.Application;
using PetSpa.Modules.Customer.Domain.Enums;

using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Infrastructure.Persistence.Configurations
{
    internal class CustomerConfiguration : IEntityTypeConfiguration<CustomerTable>
    {
        public const string UniquePhoneIndex = "IX_customer_phone";
        public const string UniqueEmailIndex = "IX_customer_email";

        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<CustomerTable> builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            builder.ToTable("customer");

            // 2. Cấu hình khóa chính.
            builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            builder.Property(x => x.FullName)
                .HasColumnName("fullname")
                .IsRequired()
                .HasMaxLength(CustomerContact.FullNameMaxLength);

            builder.Property(x => x.Gender)
                .HasColumnName("gender")
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.DateOfBirth)
                .HasColumnName("date_of_birth")
                .HasColumnType("date");

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(CustomerContact.EmailMaxLength);
            // 4. Cấu hình quan hệ hoặc chỉ mục.
            builder.HasIndex(x => x.Email).IsUnique().HasDatabaseName(UniqueEmailIndex);

            builder.Property(x => x.Phone)
                .HasColumnName("phone")
                .IsRequired()
                .HasMaxLength(CustomerContact.PhoneMaxLength);
            builder.HasIndex(x => x.Phone).IsUnique().HasDatabaseName(UniquePhoneIndex);

            builder.Property(x => x.AvtURL)
                .HasColumnName("avatar_url")
                .HasColumnType("text");

            builder.Property(x => x.Note)
                .HasColumnName("note");

            builder.Property(x => x.Status) 
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(30)
                .HasDefaultValue(CustomerStatus.ACTIVE);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

        }
    }
}
