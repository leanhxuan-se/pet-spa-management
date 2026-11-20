using Microsoft.EntityFrameworkCore;

namespace PetSpa.Modules.Auth.Infrastructure.Persistence;

internal sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    internal const string Schema = "petspa_auth";
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        var account = modelBuilder.Entity<Account>();
        account.ToTable("accounts", table => table.HasCheckConstraint("ck_accounts_profile",
            "(customer_id IS NOT NULL AND customer_id > 0 AND staff_id IS NULL) OR " +
            "(staff_id IS NOT NULL AND staff_id > 0 AND customer_id IS NULL)"));
        account.Property(x => x.Id).HasColumnName("id");
        account.Property(x => x.LoginPhone).HasColumnName("login_phone").HasMaxLength(30).IsRequired();
        account.Property(x => x.PasswordHash).HasColumnName("password_hash").IsRequired();
        account.Property(x => x.CustomerId).HasColumnName("customer_id");
        account.Property(x => x.StaffId).HasColumnName("staff_id");
        account.Property(x => x.IsEnabled).HasColumnName("is_enabled").HasDefaultValue(true);
        account.Property(x => x.TokenVersion).HasColumnName("token_version").HasDefaultValue(0L).IsConcurrencyToken();
        account.Property(x => x.LockedUntilUtc).HasColumnName("locked_until_utc");
        account.HasIndex(x => x.LoginPhone).IsUnique();
        account.HasIndex(x => x.CustomerId).IsUnique();
        account.HasIndex(x => x.StaffId).IsUnique();
    }
}
