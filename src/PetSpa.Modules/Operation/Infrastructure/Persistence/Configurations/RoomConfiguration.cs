using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    internal class RoomConfiguration : IEntityTypeConfiguration<Room>
    {
        public void Configure(EntityTypeBuilder<Room> Builder)
        {
            Builder.ToTable("room");

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.RoomName)
                .HasColumnName("room_name")
                .HasMaxLength(50)
                .IsRequired();

            Builder.Property(x => x.RoomType)
                .HasColumnName("room_type")
                .HasMaxLength(50)
                .IsRequired();

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(15);

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

        }
    }
}
