using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Resource.Domain.Entities;


namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Configurations
{
    public class ServiceConfiguration: IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> Builder)
        {
            Builder.ToTable("service", "resource");

            Builder.HasKey(x => x.Id);


            Builder.Property(x => x.CategoryId)
                .HasColumnName("category_id")
                .IsRequired();

            Builder.Property(x => x.ServiceName)
                .HasColumnName("service_name")
                .HasColumnType("character varying")
                .HasMaxLength(256)
                .IsRequired();

            Builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasColumnType("character varying")
                .IsRequired();

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            Builder .HasMany(x=> x.ServiceOptions)
                .WithOne(x => x.Service)
                .HasForeignKey(x=> x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);
                
        }
    }
}
