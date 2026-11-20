namespace PetSpa.Modules.Auth.Infrastructure.Persistence;

internal sealed class Account
{
    public long Id { get; set; }
    public string LoginPhone { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public long? CustomerId { get; set; }
    public long? StaffId { get; set; }
    public bool IsEnabled { get; set; } = true;
    public long TokenVersion { get; set; }
    public DateTimeOffset? LockedUntilUtc { get; set; }
}
