using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Auth.Infrastructure.Persistence;
using PetSpa.Modules.Auth.Infrastructure.Tokens;
using PetSpa.SharedKernel.Application.Authentication;
using PetSpa.SharedKernel.Application.Dtos;

namespace PetSpa.Modules.Auth.Application;

internal sealed class LoginService(
    AuthDbContext db,
    IPasswordHasher<Account> passwords,
    ICustomerAuthProfiles customers,
    IStaffAuthProfiles staff,
    JwtTokenService tokens)
{
    internal const string AccountIdClaim = "account_id";
    internal const string TokenVersionClaim = "token_version";

    public async Task<LoginResponse?> LoginAsync(string phone, string password, CancellationToken ct)
    {
        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.LoginPhone == phone, ct);
        var passwordResult = passwords.VerifyHashedPassword(
            account ?? DummyCredential.Account, account?.PasswordHash ?? DummyCredential.Hash, password);
        if (passwordResult == PasswordVerificationResult.Failed || account is null || !CanLogin(account))
            return null;

        var profile = await GetActiveProfileAsync(account, ct);
        if (profile is null)
            return null;

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var hash = passwords.HashPassword(account, password);
            var updated = await db.Accounts
                .Where(x => x.Id == account.Id && x.TokenVersion == account.TokenVersion && x.PasswordHash == account.PasswordHash)
                .ExecuteUpdateAsync(set => set.SetProperty(x => x.PasswordHash, hash), ct);
            if (updated == 0)
                return null;
        }

        var type = account.CustomerId.HasValue ? AccountTypes.Customer : AccountTypes.Staff;
        var id = profile.Id.ToString(CultureInfo.InvariantCulture);
        var (token, expiresAt) = tokens.Generate([
            new(AuthClaimTypes.Subject, $"{type}:{id}"),
            new(AuthClaimTypes.Role, profile.Role),
            new(AuthClaimTypes.AccountType, type),
            new(account.CustomerId.HasValue ? AuthClaimTypes.CustomerId : AuthClaimTypes.StaffId, id),
            new(AccountIdClaim, account.Id.ToString(CultureInfo.InvariantCulture)),
            new(TokenVersionClaim, account.TokenVersion.ToString(CultureInfo.InvariantCulture))
        ]);
        return new(token, "Bearer", expiresAt, profile.Role, new(profile.Id, profile.FullName, profile.Phone));
    }

    public async Task<bool> ValidateAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!principal.HasValidAccount() || !ReadSession(principal, out var accountId, out var version))
            return false;

        var account = await db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, ct);
        if (account is null || !CanLogin(account) || account.TokenVersion != version ||
            account.CustomerId != principal.GetCustomerId() || account.StaffId != principal.GetStaffId())
            return false;

        var profile = await GetActiveProfileAsync(account, ct);
        return profile is not null && principal.IsInRole(profile.Role);
    }

    public async Task<bool> LogoutAsync(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!ReadSession(principal, out var accountId, out var version))
            return false;

        return await db.Accounts.Where(x => x.Id == accountId && x.TokenVersion == version)
            .ExecuteUpdateAsync(set => set.SetProperty(x => x.TokenVersion, x => x.TokenVersion + 1), ct) == 1;
    }

    private async Task<AccountProfile?> GetActiveProfileAsync(Account account, CancellationToken ct)
    {
        if (account.CustomerId is long customerId)
        {
            var customer = await customers.GetCustomerForAuthAsync(customerId, ct);
            return customer is { IsActive: true, Role: AccountRoles.Customer } ? customer : null;
        }
        if (account.StaffId is not long staffId)
            return null;

        var employee = await staff.GetStaffForAuthAsync(staffId, ct);
        return employee is { IsActive: true, Role: AccountRoles.Staff or AccountRoles.Receptionist or AccountRoles.Manager or AccountRoles.Admin }
            ? employee : null;
    }

    private static bool CanLogin(Account account) =>
        account.IsEnabled && (account.LockedUntilUtc is null || account.LockedUntilUtc <= DateTimeOffset.UtcNow);

    private static bool ReadSession(ClaimsPrincipal principal, out long id, out long version)
    {
        static bool Read(ClaimsPrincipal principal, string type, out long value)
        {
            var claims = principal.FindAll(type).Take(2).ToArray();
            value = 0;
            return claims.Length == 1 && long.TryParse(claims[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }

        var validId = Read(principal, AccountIdClaim, out id);
        return Read(principal, TokenVersionClaim, out version) && validId && id > 0;
    }

    private static class DummyCredential
    {
        internal static readonly Account Account = new();
        internal static readonly string Hash = new PasswordHasher<Account>()
            .HashPassword(Account, Guid.NewGuid().ToString());
    }
}
