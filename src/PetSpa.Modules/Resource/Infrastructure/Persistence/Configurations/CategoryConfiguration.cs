// Cấu hình ánh xạ cơ sở dữ liệu cho Category.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            builder.ToTable("category", "resource", table =>
                table.HasCheckConstraint("ck_category_status", "status IN ('ACTIVE', 'INACTIVE')"));

            // 2. Cấu hình khóa chính.
            builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();

            // 4. Cấu hình quan hệ hoặc chỉ mục.
            builder.HasIndex(x => x.Name).IsUnique();

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(ResourceStatus.ACTIVE)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");
            
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()")
                .IsRequired();


        }
    }
}
