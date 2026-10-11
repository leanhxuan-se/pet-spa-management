using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Configurations
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("service", "resource");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CategoryId)
                .HasColumnName("category_id")
                .HasColumnType("bigint")
                .IsRequired();

            builder.Property(x => x.ServiceName)
                .HasColumnName("service_name")
                .HasColumnType("character varying")
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasColumnType("character varying")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            builder.HasOne(x => x.Category)
                .WithMany(y => y.Services)
                .HasForeignKey(x => x.CategoryId)
                .HasConstraintName("fk_service_Category")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
