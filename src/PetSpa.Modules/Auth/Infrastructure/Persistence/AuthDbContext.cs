using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Auth.Infrastructure.Identity;
using PetSpa.Modules.Auth.Infrastructure.Tokens;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Auth.Infrastructure.Persistence;

internal sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : IdentityDbContext<AppUser>(options)
{
    internal const string Schema = "petspa_auth";

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);

        var user = modelBuilder.Entity<AppUser>();
        user.ToTable("AspNetUsers", table =>
        {
            table.HasCheckConstraint("ck_auth_user_phone", """
                "PhoneNumber" <> '' AND "PhoneNumber" !~ '^[[:space:]]|[[:space:]]$'
                """);
            table.HasCheckConstraint("ck_auth_user_profile", $"""
                ("AccountType" = '{AccountTypes.Customer}' AND "StaffId" IS NULL AND ("CustomerId" IS NULL OR "CustomerId" > 0))
                OR ("AccountType" = '{AccountTypes.Staff}' AND "CustomerId" IS NULL AND ("StaffId" IS NULL OR "StaffId" > 0))
                """);
            table.HasCheckConstraint("ck_auth_user_provisioning", $"""
                "ProvisioningState" = '{nameof(AccountProvisioningState.Pending)}'
                OR ("ProvisioningState" = '{nameof(AccountProvisioningState.Ready)}'
                    AND ("CustomerId" IS NOT NULL OR "StaffId" IS NOT NULL))
                """);
            table.HasCheckConstraint("ck_auth_user_token_version", """
                "TokenVersion" >= 0
                """);
        });
        user.Property(account => account.AccountType).HasMaxLength(8).IsRequired();
        user.Property(account => account.PhoneNumber).HasMaxLength(30).IsRequired();
        user.Property(account => account.UserName).IsRequired();
        user.Property(account => account.NormalizedUserName).IsRequired();
        user.Property(account => account.ProvisioningState).HasConversion<string>().HasMaxLength(7);
        user.Property(account => account.TokenVersion).IsConcurrencyToken();
        user.HasIndex(account => new { account.AccountType, account.PhoneNumber }).IsUnique();
        user.HasIndex(account => account.CustomerId).IsUnique().HasFilter("""
            "CustomerId" IS NOT NULL
            """);
        user.HasIndex(account => account.StaffId).IsUnique().HasFilter("""
            "StaffId" IS NOT NULL
            """);

        modelBuilder.Entity<IdentityUserRole<string>>().HasIndex(role => role.UserId).IsUnique();
        string[] roles =
        [
            AccountRoles.Customer, AccountRoles.Staff, AccountRoles.Receptionist, AccountRoles.Manager, AccountRoles.Admin
        ];
        modelBuilder.Entity<IdentityRole>().HasData(roles.Select(role => new
        {
            Id = role,
            Name = role,
            NormalizedName = role.ToUpperInvariant()
        }));

        var token = modelBuilder.Entity<RefreshToken>();
        token.ToTable("refresh_tokens", table =>
        {
            table.HasCheckConstraint("ck_refresh_token_hashes", """
                octet_length("TokenHash") = 32
                AND ("ReplacedByTokenHash" IS NULL OR octet_length("ReplacedByTokenHash") = 32)
                """);
            table.HasCheckConstraint("ck_refresh_token_expiration", """
                "ExpiresAtUtc" > "CreatedAtUtc"
                """);
        });
        token.HasOne<AppUser>().WithMany().HasForeignKey(refresh => refresh.UserId);
        token.HasIndex(refresh => refresh.TokenHash).IsUnique();
        token.HasIndex(refresh => refresh.FamilyId);
        token.HasIndex(refresh => refresh.ExpiresAtUtc);
    }
}
