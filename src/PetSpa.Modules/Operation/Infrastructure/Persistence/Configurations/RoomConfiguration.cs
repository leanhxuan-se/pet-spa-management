// Cấu hình ánh xạ cơ sở dữ liệu cho Room.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    internal class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<Room> Builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            Builder.ToTable("room", table =>
                table.HasCheckConstraint("ck_room_status", "status IN ('AVAILABLE', 'UNAVAILABLE', 'MAINTENANCE')"));

            // 2. Cấu hình khóa chính.
            Builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            Builder.Property(x => x.RoomName)
                .HasColumnName("room_name")
                .HasMaxLength(100)
                .IsRequired();

            // 4. Cấu hình quan hệ hoặc chỉ mục.
            Builder.HasIndex(x => x.RoomName).IsUnique();

            Builder.Property(x => x.RoomType)
                .HasColumnName("room_type")
                .HasMaxLength(50);

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasDefaultValue(RoomStatus.AVAILABLE)
                .IsRequired()
                .HasMaxLength(30);

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

        }
    }
}
