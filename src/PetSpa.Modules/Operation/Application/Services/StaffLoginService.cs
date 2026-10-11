using PetSpa.Modules.Operation.Application.Abstractions;
using PetSpa.Modules.Operation.Application.Dtos;
using PetSpa.Modules.Operation.Domain.Enums;
using PetSpa.Modules.Operation.Domain.Repositories;

namespace PetSpa.Modules.Operation.Application.Services;

public class StaffLoginService(
    IStaffRepository staffs,
    IPasswordService passwordService)
{
    public async Task<StaffLoginResult?> LoginAsync(
        string phone,
        string password,
        CancellationToken ct)
    {
        var normalizedPhone = phone.Trim();

        // 1. Tìm nhân viên theo số điện thoại.
        var staff = await staffs.GetByPhoneAsync(normalizedPhone, ct);

        // 2. Kiểm tra tài khoản có được phép đăng nhập.
        if (staff is null ||
            staff.Status != StaffStatus.ACTIVE ||
            string.IsNullOrEmpty(staff.PasswordHash))
        {
            return null;
        }

        // 3. So sánh mật khẩu nhập vào với hash đã lưu.
        var verification = passwordService.VerifyHashedPassword(
            staff,
            staff.PasswordHash,
            password);

        if (verification == PasswordCheckResult.Failed)
        {
            return null;
        }

        // 4. Nâng cấp hash nếu cấu hình băm đã thay đổi.
        if (verification == PasswordCheckResult.SuccessRehashNeeded)
        {
            staff.PasswordHash = passwordService.HashPassword(
                staff,
                password);
        }

        // 5. Lưu thời điểm đăng nhập thành công.
        staff.LastLoginAt = DateTime.UtcNow;
        await staffs.SaveAsync(ct);

        return new StaffLoginResult(
            staff.Id,
            staff.FullName,
            staff.Phone,
            staff.Role);
    }
}
