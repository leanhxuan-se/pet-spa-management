// Định nghĩa hoặc xử lý thông tin xác thực ClaimsPrincipalExtensions.
using System.Globalization;
using System.Security.Claims;

namespace PetSpa.SharedKernel.Application.Authentication;

public static class ClaimsPrincipalExtensions
{
    // Kiểm tra thông tin tài khoản hợp lệ.
    public static bool HasValidAccount(this ClaimsPrincipal user) => ReadAccount(user) is not null;

    // Kiểm tra tài khoản nhân viên hợp lệ.
    public static bool HasStaffRole(this ClaimsPrincipal user) =>
        ReadAccount(user) is { Type: AccountTypes.Staff };

    // Lấy mã khách hàng từ tài khoản.
    public static long? GetCustomerId(this ClaimsPrincipal user) =>
        ReadAccount(user) is { Type: AccountTypes.Customer } account ? account.Id : null;

    // Kiểm tra quyền quản lý khách hàng.
    public static bool CanManageCustomers(this ClaimsPrincipal user) =>
        ReadAccount(user) is { Role: AccountRoles.Receptionist or AccountRoles.Manager or AccountRoles.Admin };

    // Kiểm tra quyền truy cập khách hàng.
    public static bool CanAccessCustomer(this ClaimsPrincipal user, long customerId) =>
        customerId > 0 && ReadAccount(user) switch
        {
            { Role: AccountRoles.Receptionist or AccountRoles.Manager or AccountRoles.Admin } => true,
            { Type: AccountTypes.Customer } account => account.Id == customerId,
            _ => false
        };

    // Đọc và kiểm tra các claim tài khoản.
    private static (string Type, string Role, long Id)? ReadAccount(ClaimsPrincipal user)
    {
        // 1. Kiểm tra tài khoản đã xác thực.
        if (user.Identity is not ClaimsIdentity { IsAuthenticated: true, RoleClaimType: AuthClaimTypes.Role } identity)
            return null;

        // 2. Đọc vai trò và loại tài khoản.
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

        // 3. Kiểm tra mã tài khoản và định danh trong token.
        var otherIdClaim = idClaim == AuthClaimTypes.CustomerId ? AuthClaimTypes.StaffId : AuthClaimTypes.CustomerId;
        var rawId = SingleValue(identity, idClaim);
        if (identity.HasClaim(claim => claim.Type == otherIdClaim) ||
            !long.TryParse(rawId, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ||
            id <= 0 || SingleValue(identity, AuthClaimTypes.Subject) !=
                $"{accountType}:{id.ToString(CultureInfo.InvariantCulture)}")
            return null;

        // 4. Trả thông tin tài khoản hợp lệ.
        return (accountType, role, id);
    }

    // Lấy giá trị claim duy nhất.
    private static string? SingleValue(ClaimsIdentity identity, string claimType)
    {
        // 1. Tìm các claim theo loại.
        using var claims = identity.Claims.Where(claim => claim.Type == claimType).GetEnumerator();
        if (!claims.MoveNext())
            return null;

        // 2. Chỉ trả giá trị khi có đúng một claim.
        var value = claims.Current.Value;
        return claims.MoveNext() ? null : value;
    }
}
