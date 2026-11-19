using Microsoft.AspNetCore.Identity;

namespace PetSpa.Modules.Auth.Infrastructure.Identity;

internal sealed class AppUser : IdentityUser
{
    public string AccountType { get; set; } = string.Empty;
    public long? CustomerId { get; set; }
    public long? StaffId { get; set; }
    public bool IsActive { get; set; }
    public AccountProvisioningState ProvisioningState { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public int TokenVersion { get; set; }
}

internal enum AccountProvisioningState
{
    Pending,
    Ready
}
