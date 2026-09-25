using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    public class StaffConfiguration : IEntityTypeConfiguration<Staff>
    {
        public void Configure(EntityTypeBuilder<Staff> Builder)
        {
            Builder.ToTable("staff");

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.FullName)
                .HasColumnName("fullname")
                .IsRequired()
                .HasMaxLength(50);

            Builder.Property(x => x.Gender)
                .HasColumnName("gender")
                .HasMaxLength(10);

            Builder.Property(x => x.DateOfBirth)
                .HasColumnName("date_of_birth")
                .HasColumnType("date");

            Builder.Property(x => x.Email)
                .HasColumnName("email")
                .IsRequired()
                .HasMaxLength(50);
            Builder.HasIndex(x => x.Email).IsUnique();

            Builder.Property(x => x.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20);
            Builder.HasIndex(x => x.Phone).IsUnique();

            Builder.Property(x => x.Position)
                .HasColumnName("position")
                .HasMaxLength(50);

            Builder.Property(x => x.HiredDate)
                .HasColumnName("hired_date")
                .HasColumnType("date");

            Builder.Property(x => x.PasswordHash)
                .HasColumnName("password_hash")
                .IsRequired()
                .HasMaxLength(128);

            Builder.Property(x => x.AvtURL)
                .HasColumnName("avt_url")
                .HasMaxLength(512);

            Builder.Property(x => x.Note)
                .HasColumnName("note");

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasMaxLength(20);

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");

            Builder.Property(x => x.LastLoginAt)
                .HasColumnName("last_login_at")
                .HasColumnType("timestamp with time zone");
        }
    }
}
