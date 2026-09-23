using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Customer.Domain.Entities;

namespace PetSpa.Modules.Customer.Infrastructure.Persistence.Configurations
{
    public class PetConfiguration: IEntityTypeConfiguration<Pet>
    {
        public void Configure(EntityTypeBuilder<Pet> builder)
        {
            builder.ToTable("pet");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.CustomersId)
                .HasColumnName("customer_id");

            builder.HasOne(x => x.Customer)
                .WithMany(y => y.Pets)
                .HasForeignKey(z => z.CustomersId)
                .HasConstraintName("fk_pet_customer")
                .OnDelete(DeleteBehavior.Cascade); // auto delete Pet when Customer is deleted

            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Species)
                .HasColumnName("species")
                .HasMaxLength(50)
                .IsRequired();

            // No minValue so we will validate value later in Application.validator

            builder.Property(x => x.Weight)
                .HasColumnName("weight");

            builder.Property(x => x.Height)
                .HasColumnName("height");

            builder.Property(x => x.Gender)
                .HasColumnName("gender");

            builder.Property(x => x.DateOfBirth)
                .HasColumnName("date_of_birth")
                .HasColumnType("date");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasMaxLength(10)
                .IsRequired();

            builder.Property(x => x.SpecialNote)
                .HasColumnName("special_note")
                .HasColumnType("text");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");
        }
    }
}
