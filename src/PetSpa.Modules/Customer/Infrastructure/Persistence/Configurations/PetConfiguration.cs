// Cấu hình ánh xạ cơ sở dữ liệu cho Pet.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Customer.Domain.Entities;
using PetSpa.Modules.Customer.Domain.Enums;

namespace PetSpa.Modules.Customer.Infrastructure.Persistence.Configurations
{
    internal class PetConfiguration: IEntityTypeConfiguration<Pet>
    {
        public const int NameMaxLength = 100;

        // Cấu hình ánh xạ thực thể vào cơ sở dữ liệu.
        public void Configure(EntityTypeBuilder<Pet> builder)
        {
            // 1. Cấu hình tên bảng và các ràng buộc.
            builder.ToTable("pet", table =>
            {
                table.HasCheckConstraint("ck_pet_weight_positive", "weight > 0");
                table.HasCheckConstraint("ck_pet_height_positive", "height > 0");
            });

            // 2. Cấu hình khóa chính.
            builder.HasKey(x => x.Id);

            // 3. Cấu hình các cột và kiểu dữ liệu.
            builder.Property(x => x.CustomersId)
                .HasColumnName("customer_id");


            // Create 1 : N relationship Customer : Pet
            builder.HasOne(x => x.Customer)
                .WithMany(y => y.Pets)
                .HasForeignKey(z => z.CustomersId)
                .HasConstraintName("fk_pet_customer")
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.Name)
                .HasColumnName("name")
                .HasMaxLength(NameMaxLength)
                .IsRequired();

            builder.Property(x => x.Species)
                .HasColumnName("species")
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Weight)
                .HasColumnName("weight")
                .HasPrecision(6,2);

            builder.Property(x => x.Height)
                .HasColumnName("height")
                .HasPrecision(6, 2);

            builder.Property(x => x.Gender)
                .HasColumnName("gender")
                .HasConversion<string>()
                .HasMaxLength(20);

            builder.Property(x => x.DateOfBirth)
                .HasColumnName("date_of_birth")
                .HasColumnType("date");

            builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired()
                .HasDefaultValue(PetStatus.ACTIVE);

            builder.Property(x => x.SpecialNote)
                .HasColumnName("special_note")
                .HasColumnType("text");

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");
        }
    }
}
