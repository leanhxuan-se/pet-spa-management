// Cấu hình ánh xạ cơ sở dữ liệu cho ServiceOption.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Resource.Domain.Entities;
using PetSpa.Modules.Resource.Domain.Enums;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Configurations
{
    public class ServiceOptionConfiguration : IEntityTypeConfiguration<ServiceOption>
    {
        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<ServiceOption> builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            builder.ToTable("service_options", "resource", table =>
            {
                table.HasCheckConstraint("ck_service_options_status", "status IN ('ACTIVE', 'INACTIVE')");
                table.HasCheckConstraint("ck_service_options_min_weight", "min_weight >= 0");
                table.HasCheckConstraint("ck_service_options_max_weight", "max_weight >= 0");
                table.HasCheckConstraint("ck_service_options_weight_range",
                    "min_weight IS NULL OR max_weight IS NULL OR max_weight >= min_weight");
                table.HasCheckConstraint("ck_service_options_duration", "duration_minutes > 0");
                table.HasCheckConstraint("ck_service_options_unit_price", "unit_price >= 0");
            });

            // 2. Cấu hình khóa chính.
            builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(ResourceStatus.ACTIVE)
                .IsRequired();

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            // 4. Cấu hình quan hệ hoặc chỉ mục.
            builder.HasIndex(x => x.ServiceId);

            builder.Property(x => x.ServiceId)
                .HasColumnName("service_id")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.Species)
                .HasColumnName("species")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.MinWeight)
                .HasColumnName("min_weight")
                .HasPrecision(6, 2)
                .IsRequired(false);

            builder.Property(x => x.MaxWeight)
                .HasColumnName("max_weight")
                .HasPrecision(6, 2)
                .IsRequired(false);

            builder.Property(x => x.DurationMinutes)
                .HasColumnName("duration_minutes")
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.UnitPrice)
                .HasColumnName("unit_price")
                .HasPrecision(18, 2)
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

            builder.HasOne(x => x.Service)
                .WithMany(y => y.ServiceOptions)
                .HasForeignKey(x => x.ServiceId)
                .HasConstraintName("fk_service_option_service")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
