// Cấu hình ánh xạ cơ sở dữ liệu cho Service.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Configurations
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            builder.ToTable("service", "resource", table =>
                table.HasCheckConstraint("ck_service_status", "status IN ('ACTIVE', 'INACTIVE')"));

            // 2. Cấu hình khóa chính.
            builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            // 4. Cấu hình quan hệ hoặc chỉ mục.
            builder.HasIndex(x => x.CategoryId);
            builder.HasIndex(x => new { x.CategoryId, x.ServiceName }).IsUnique();

            builder.Property(x => x.CategoryId)
                .HasColumnName("category_id")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.ServiceName)
                .HasColumnName("service_name")
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(ResourceStatus.ACTIVE)
                .IsRequired();

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

            builder.HasOne(x => x.Category)
                .WithMany(y => y.Services)
                .HasForeignKey(x => x.CategoryId)
                .HasConstraintName("fk_service_category")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
