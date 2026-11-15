using System.Globalization;
using System.Security.Claims;

namespace PetSpa.SharedKernel.Application.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static bool HasValidAccount(this ClaimsPrincipal user) => ReadAccount(user) is not null;

    public static bool HasStaffRole(this ClaimsPrincipal user) =>
        ReadAccount(user) is { Type: AccountTypes.Staff };

    public static long? GetCustomerId(this ClaimsPrincipal user) =>
        ReadAccount(user) is { Type: AccountTypes.Customer } account ? account.Id : null;

    public static bool CanManageCustomers(this ClaimsPrincipal user) =>
        ReadAccount(user) is { Role: AccountRoles.Receptionist or AccountRoles.Manager or AccountRoles.Admin };

    public static bool CanAccessCustomer(this ClaimsPrincipal user, long customerId) =>
        customerId > 0 && ReadAccount(user) switch
        {
            { Role: AccountRoles.Receptionist or AccountRoles.Manager or AccountRoles.Admin } => true,
            { Type: AccountTypes.Customer } account => account.Id == customerId,
            _ => false
        };

    private static (string Type, string Role, long Id)? ReadAccount(ClaimsPrincipal user)
    {
        if (user.Identity is not ClaimsIdentity { IsAuthenticated: true, RoleClaimType: AuthClaimTypes.Role } identity)
            return null;

        var role = SingleValue(identity, AuthClaimTypes.Role);
        var accountType = SingleValue(identity, AuthClaimTypes.AccountType);
        var idClaim = (accountType, role) switch
        {
            (AccountTypes.Customer, AccountRoles.Customer) => AuthClaimTypes.CustomerId,
            (AccountTypes.Staff, AccountRoles.Staff or AccountRoles.Receptionist or AccountRoles.Manager or AccountRoles.Admin)
                => AuthClaimTypes.StaffId,
            _ => null
        };

        if (idClaim is null || role is null || accountType is null || !user.IsInRole(role))
            return null;

        var otherIdClaim = idClaim == AuthClaimTypes.CustomerId ? AuthClaimTypes.StaffId : AuthClaimTypes.CustomerId;
        var rawId = SingleValue(identity, idClaim);
        if (identity.HasClaim(claim => claim.Type == otherIdClaim) ||
            !long.TryParse(rawId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ||
            id <= 0 || SingleValue(identity, AuthClaimTypes.Subject) !=
                $"{accountType}:{id.ToString(CultureInfo.InvariantCulture)}")
            return null;

        return (accountType, role, id);
    }

    private static string? SingleValue(ClaimsIdentity identity, string claimType)
    {
        using var claims = identity.Claims.Where(claim => claim.Type == claimType).GetEnumerator();
        if (!claims.MoveNext())
            return null;

        var value = claims.Current.Value;
        return claims.MoveNext() ? null : value;
    }
}
