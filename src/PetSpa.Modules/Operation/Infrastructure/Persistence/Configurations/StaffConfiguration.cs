using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetSpa.Modules.Operation.Domain.Entities;
using PetSpa.Modules.Operation.Domain.Enums;

namespace PetSpa.Modules.Operation.Infrastructure.Persistence.Configurations
{
    public class StaffConfiguration : IEntityTypeConfiguration<Staff>
    {
        public void Configure(EntityTypeBuilder<Staff> Builder)
        {
            Builder.ToTable("staff", table =>
            {
                table.HasCheckConstraint("ck_staff_gender", "gender IN ('MALE', 'FEMALE', 'OTHER')");
                table.HasCheckConstraint("ck_staff_role", "role IN ('MANAGER', 'RECEPTIONIST', 'STAFF')");
                table.HasCheckConstraint("ck_staff_status", "status IN ('ACTIVE', 'INACTIVE')");
            });

            Builder.HasKey(x => x.Id);

            Builder.Property(x => x.Id)
                .HasColumnName("id")
                .HasColumnType("bigint")
                .UseIdentityByDefaultColumn();

            Builder.Property(x => x.FullName)
                .HasColumnName("fullname")
                .IsRequired()
                .HasMaxLength(150);

            Builder.Property(x => x.Gender)
                .HasColumnName("gender")
                .HasConversion<string>()
                .HasMaxLength(20);

            Builder.Property(x => x.DateOfBirth)
                .HasColumnName("date_of_birth")
                .HasColumnType("date");

            Builder.Property(x => x.Email)
                .HasColumnName("email")
                .IsRequired()
                .HasMaxLength(255);
            Builder.HasIndex(x => x.Email).IsUnique();

            Builder.Property(x => x.Phone)
                .HasColumnName("phone")
                .HasMaxLength(30);
            Builder.HasIndex(x => x.Phone).IsUnique();

            Builder.Property(x => x.Position)
                .HasColumnName("position")
                .IsRequired()
                .HasMaxLength(100);

            Builder.Property(x => x.Role)
                .HasColumnName("role")
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(30);

            Builder.Property(x => x.HiredDate)
                .HasColumnName("hired_date")
                .HasColumnType("date");

            Builder.Property(x => x.PasswordHash)
                .HasColumnName("password_hash")
                .HasMaxLength(255)
                .IsRequired();

            Builder.Property(x => x.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasColumnType("text");

            Builder.Property(x => x.Note)
                .HasColumnName("note")
                .HasColumnType("text");

            Builder.Property(x => x.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasDefaultValue(StaffStatus.ACTIVE)
                .IsRequired()
                .HasMaxLength(30);

            Builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            Builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("NOW()");

            Builder.Property(x => x.LastLoginAt)
                .HasColumnName("last_login_at")
                .HasColumnType("timestamp with time zone");
        }
    }
}
