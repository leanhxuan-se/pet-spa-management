using System.Security.Claims;

namespace PetSpa.SharedKernel.Application.Authentication;

public sealed record AccountProfile(long Id, string FullName, string Phone, bool IsActive, string Role);

public interface ICustomerAuthProfiles
{
    Task<AccountProfile?> FindForRegistrationByPhoneAsync(string normalizedPhone, CancellationToken ct);
    Task<AccountProfile?> GetCustomerForAuthAsync(long customerId, CancellationToken ct);
}

public interface IStaffAuthProfiles
{
    Task<AccountProfile?> GetStaffForAuthAsync(long staffId, CancellationToken ct);
    Task<long[]> GetActiveAdminIdsAsync(CancellationToken ct);
}

public interface IStaffAccountGuard
{
    Task<bool> CanActAsync(ClaimsPrincipal actor, CancellationToken ct);
    Task<bool> CanDeactivateAdminAsync(long staffId, CancellationToken ct);
}

public static class StaffAccountAdministration
{
    // Shared by PostgreSQL transactions changing staff roles, status or accounts.
    public const long AdvisoryLockKey = 7200151037;

    public static bool CanManageRole(string? actorRole, string targetRole) =>
        actorRole == AccountRoles.Admin ||
        actorRole == AccountRoles.Manager && targetRole is AccountRoles.Staff or AccountRoles.Receptionist;
}
