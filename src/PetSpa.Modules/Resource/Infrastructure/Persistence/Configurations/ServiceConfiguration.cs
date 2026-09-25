using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Resource.Domain.Entities;


namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Configurations
{
    public class ServiceConfiguration: IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> Builder)
        {
            Builder.ToTable("service");

            Builder.HasKey(x => x.Id);

            // Create 1 : N relationship Category : Service
            Builder.HasOne(x => x.Category)
                .WithMany(y => y.Services)
                .HasForeignKey(z => z.CategoryId)
                .HasConstraintName("fk_service_category") 
                .OnDelete(DeleteBehavior.Cascade);// auto delete service when Category is deleted


            Builder.Property(x => x.CategoryId)
                .HasColumnName("category_id")
                .IsRequired();

            Builder.Property(x => x.ServiceName)
                .HasColumnName("service_name")
                .HasMaxLength(256)
                .IsRequired();

            Builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired();

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");
                
        }
    }
}
