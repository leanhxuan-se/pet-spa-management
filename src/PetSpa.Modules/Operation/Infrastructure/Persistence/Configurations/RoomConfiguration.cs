using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    internal class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> Builder)
        {
            Builder.ToTable("room", table =>
                table.HasCheckConstraint("ck_room_status", "status IN ('AVAILABLE', 'UNAVAILABLE', 'MAINTENANCE')"));

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            Builder.Property(x => x.RoomName)
                .HasColumnName("room_name")
                .HasMaxLength(100)
                .IsRequired();

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
