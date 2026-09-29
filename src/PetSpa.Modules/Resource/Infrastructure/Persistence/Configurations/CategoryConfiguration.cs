using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Resource.Domain.Entities;

namespace PetSpa.Modules.Resource.Infrastructure.Persistence.Configurations
{
    public class CategoryConfiguration: IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> Builder)
        {
            Builder.ToTable("category","resource");

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasColumnType("character varying")
                .HasMaxLength(50)
                .IsRequired();

            Builder.Property(x => x.Description)
                .HasColumnName("description")
                .HasColumnType("text");
            
            Builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

            Builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

            Builder.HasMany(x=> x.Services)
            .WithOne(x=>x.Category)
            .HasForeignKey(x=>x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
