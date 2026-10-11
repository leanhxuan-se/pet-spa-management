using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Configurations
{
    public class ServiceOptionConfiguration : IEntityTypeConfiguration<ServiceOption>
    {
        public void Configure(EntityTypeBuilder<ServiceOption> builder)
        {
            builder.ToTable("service_options", "resource");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasColumnType("character varying")
                .IsRequired();

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.ServiceId)
                .HasColumnName("service_id")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.Species)
                .HasColumnName("species")
                .HasConversion<string>()
                .HasColumnType("character varying")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.MinWeight)
                .HasColumnName("min_weight")
                .HasColumnType("numeric")
                .IsRequired(false);

            builder.Property(x => x.MaxWeight)
                .HasColumnName("max_weight")
                .HasColumnType("numeric")
                .IsRequired(false);

            builder.Property(x => x.DurationMinutes)
                .HasColumnName("duration_minutes")
                .HasColumnType("integer")
                .IsRequired();

            builder.Property(x => x.UnitPrice)
                .HasColumnName("unit_price")
                .HasColumnType("numeric")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasOne(x => x.Service)
                .WithMany(y => y.ServiceOptions)
                .HasForeignKey(x => x.ServiceId)
                .HasConstraintName("fk_service_option_Service")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
