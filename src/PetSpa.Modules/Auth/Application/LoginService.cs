// Xác thực mật khẩu và phát token theo hợp đồng đăng nhập hiện tại.
using System.Globalization;
using System.Security.Claims;
using PetSpa.Modules.Auth.Contracts;
using PetSpa.Modules.Auth.Infrastructure.Tokens;
using PetSpa.SharedKernel.Application.Abstractions;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Auth.Application;

internal sealed class LoginService(IPasswordService<LegacyLoginAccount> passwords, JwtTokenService tokens)
{
    // Xác thực tài khoản theo loại và trả token khi đăng nhập thành công.
    public async Task<LoginResponse?> LoginAsync(
        string accountType, ILegacyLoginStore store, string phone, string password, CancellationToken ct)
    {
        // 1. Tìm tài khoản đang hoạt động của đúng loại.
        var account = await store.FindActiveByPhoneAsync(phone.Trim(), ct);
        if (account is null || string.IsNullOrEmpty(account.PasswordHash) || !HasValidRole(accountType, account.Role))
            return null;

        // 2. Kiểm tra mật khẩu và nâng cấp hash cũ nếu cần.
        var verification = passwords.VerifyHashedPassword(account, account.PasswordHash, password);
        if (verification == PasswordCheckResult.Failed)
            return null;

        var passwordHash = verification == PasswordCheckResult.SuccessRehashNeeded
            ? passwords.HashPassword(account, password)
            : account.PasswordHash;

        // 3. Ghi nhận đăng nhập trước khi phát token.
        if (!await store.RecordLoginAsync(account, passwordHash, DateTime.UtcNow, ct))
            return null;

        // 4. Tạo claim, phát token và chuyển thông tin tài khoản sang phản hồi.
        var id = account.Id.ToString(CultureInfo.InvariantCulture);
        var idClaim = accountType == AccountTypes.Customer ? AuthClaimTypes.CustomerId : AuthClaimTypes.StaffId;
        var token = tokens.Generate(
        [
            new Claim(AuthClaimTypes.Subject, $"{accountType}:{id}"),
            new Claim(AuthClaimTypes.AccountType, accountType),
            new Claim(AuthClaimTypes.Role, account.Role),
            new Claim(idClaim, id)
        ]);

        return new LoginResponse(token.AccessToken, "Bearer", token.ExpiresAtUtc, account.Role,
            new LoginUser(account.Id, account.FullName, account.Phone));
    }

    // Kiểm tra vai trò có thuộc loại tài khoản đang đăng nhập hay không.
    private static bool HasValidRole(string accountType, string role) => (accountType, role) switch
    {
        (AccountTypes.Customer, AccountRoles.Customer) => true,
        (AccountTypes.Staff, AccountRoles.Staff or AccountRoles.Receptionist or AccountRoles.Manager or AccountRoles.Admin) => true,
        _ => false
    };
}
