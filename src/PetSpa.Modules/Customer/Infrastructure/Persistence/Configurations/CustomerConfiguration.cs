using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using CustomerTable = PetSpa.Modules.Customer.Domain.Entities.Customer;

namespace PetSpa.Modules.Customer.Infrastructure.Persistence.Configurations
{
    public class CustomerConfiguration : IEntityTypeConfiguration<CustomerTable>
    {
        public void Configure(EntityTypeBuilder<CustomerTable> builder)
        {
            builder.ToTable("customer");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FullName)
                .HasColumnName("fullname")
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.Gender)
                .HasColumnName("gender")
                .HasConversion<string>()
                .HasMaxLength(10);

            builder.Property(x => x.DateOfBirth)
                .HasColumnName("date_of_birth")
                .HasColumnType("date");

            builder.Property(x => x.Email)
                .HasColumnName("email")
                .HasMaxLength(50);
            builder.HasIndex(x => x.Email).IsUnique();

            builder.Property(x => x.Phone)
                .HasColumnName("phone")
                .IsRequired()
                .HasMaxLength(20);
            builder.HasIndex(x => x.Phone).IsUnique();

            builder.Property(x => x.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(128);

            builder.Property(x => x.AvtURL)
                .HasColumnName("avt_url")
                .HasMaxLength(512);

            builder.Property(x => x.Note)
                .HasColumnName("note");

            builder.Property(x => x.Status) 
                .HasColumnName("status")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            builder.Property(x=> x.LastLoginAt)
                .HasColumnName("last_login_at")
                .HasColumnType("timestamp with time zone");
        }
    }
}
