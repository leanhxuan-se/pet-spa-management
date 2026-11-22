using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PetSpa.Modules.Auth.Infrastructure.Persistence;
using PetSpa.Modules.Auth.Infrastructure.Tokens;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Auth.Application.Services;

internal sealed class AccountService(
    AuthDbContext db,
    IPasswordHasher<Account> passwords,
    ICustomerAuthProfiles customers,
    IStaffAuthProfiles staff,
    ActivationTokens activation,
    LoginService sessions) : IStaffAccountGuard
{
    public async Task<string?> IssueActivationAsync(long customerId, CancellationToken ct)
    {
        var profile = await customers.GetCustomerForAuthAsync(customerId, ct);
        if (profile is not { IsActive: true } || !AccountPhone.IsVietnameseMobile(profile.Phone) ||
            await db.Accounts.AnyAsync(x => x.CustomerId == customerId, ct))
            return null;

        return activation.Issue(customerId, AccountPhone.Normalize(profile.Phone));
    }

    public async Task<AccountChange> RegisterAsync(string phone, string password, string token, CancellationToken ct)
    {
        var profile = await customers.FindForRegistrationByPhoneAsync(phone, ct);
        if (profile is not { IsActive: true } || !activation.Matches(token, profile.Id, phone))
            return new(AccountChangeStatus.Invalid);

        return await InsertAsync(new Account { LoginPhone = phone, CustomerId = profile.Id }, password, ct);
    }

    public async Task<AccountChange> CreateStaffAsync(long staffId, string password, ClaimsPrincipal actor, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockStaffChangesAsync(ct);
        if (!await CanActAsync(actor, ct))
            return new(AccountChangeStatus.Forbidden);

        var profile = await staff.GetStaffForAuthAsync(staffId, ct);
        if (profile is null)
            return new(AccountChangeStatus.Missing);
        if (!StaffAccountAdministration.CanManageRole(actor.FindFirst(AuthClaimTypes.Role)?.Value, profile.Role))
            return new(AccountChangeStatus.Forbidden);
        if (!profile.IsActive)
            return new(AccountChangeStatus.Conflict);

        var phone = AccountPhone.Normalize(profile.Phone);
        if (phone.Length is 0 or > 30)
            return new(AccountChangeStatus.Invalid);

        var result = await InsertAsync(new Account { LoginPhone = phone, StaffId = staffId }, password, ct);
        if (result.Status == AccountChangeStatus.Success)
            await transaction.CommitAsync(ct);
        return result;
    }

    public async Task<AccountChangeStatus> UpdateStaffAsync(
        long staffId, string? password, bool? isEnabled, ClaimsPrincipal actor, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockStaffChangesAsync(ct);
        if (!await CanActAsync(actor, ct))
            return AccountChangeStatus.Forbidden;

        var profile = await staff.GetStaffForAuthAsync(staffId, ct);
        if (profile is null)
            return AccountChangeStatus.Missing;
        if (!StaffAccountAdministration.CanManageRole(actor.FindFirst(AuthClaimTypes.Role)?.Value, profile.Role))
            return AccountChangeStatus.Forbidden;
        if (isEnabled == true && !profile.IsActive)
            return AccountChangeStatus.Conflict;

        var account = await db.Accounts.SingleOrDefaultAsync(x => x.StaffId == staffId, ct);
        if (account is null)
            return AccountChangeStatus.Missing;
        if (isEnabled == false && profile is { Role: AccountRoles.Admin, IsActive: true } &&
            !await CanDeactivateAdminAsync(staffId, ct))
            return AccountChangeStatus.Conflict;

        if (password is not null)
            account.PasswordHash = passwords.HashPassword(account, password);
        if (isEnabled.HasValue)
            account.IsEnabled = isEnabled.Value;
        if (isEnabled == true || password is not null)
            account.LockedUntilUtc = null;
        account.TokenVersion++;
        account.UpdatedAt = DateTimeOffset.UtcNow;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return AccountChangeStatus.Conflict;
        }
        await transaction.CommitAsync(ct);
        return AccountChangeStatus.Success;
    }

    public async Task<bool> CanDeactivateAdminAsync(long staffId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var activeAdmins = await staff.GetActiveAdminIdsAsync(ct);
        var enabledAdmins = db.Accounts.Where(x => x.StaffId.HasValue && activeAdmins.Contains(x.StaffId.Value)
            && x.IsEnabled && (x.LockedUntilUtc == null || x.LockedUntilUtc <= now));
        return !await enabledAdmins.AnyAsync(x => x.StaffId == staffId, ct) ||
            await enabledAdmins.AnyAsync(x => x.StaffId != staffId, ct);
    }

    public async Task<bool> CanActAsync(ClaimsPrincipal actor, CancellationToken ct) =>
        actor.CanManageStaff() && await sessions.ValidateAsync(actor, ct);

    private Task LockStaffChangesAsync(CancellationToken ct) => db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_advisory_xact_lock({StaffAccountAdministration.AdvisoryLockKey})", ct);

    private async Task<AccountChange> InsertAsync(Account account, string password, CancellationToken ct)
    {
        account.PasswordHash = passwords.HashPassword(account, password);
        db.Accounts.Add(account);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            db.Entry(account).State = EntityState.Detached;
            return new(AccountChangeStatus.Conflict);
        }

        return new(AccountChangeStatus.Success, account.CustomerId ?? account.StaffId);
    }
}

internal enum AccountChangeStatus { Success, Invalid, Missing, Conflict, Forbidden }

internal sealed record AccountChange(AccountChangeStatus Status, long? ProfileId = null);
