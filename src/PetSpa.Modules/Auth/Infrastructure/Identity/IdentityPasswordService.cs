// Xử lý nghiệp vụ và cung cấp dịch vụ IdentityPasswordService.
using Microsoft.AspNetCore.Identity;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.Modules.Auth.Infrastructure.Identity;

internal sealed class IdentityPasswordService<TAccount>(
    IPasswordHasher<TAccount> passwordHasher) : IPasswordService<TAccount>
    where TAccount : class
{
    // Băm mật khẩu trước khi lưu.
    public string HashPassword(TAccount account, string password)
        => passwordHasher.HashPassword(account, password);

    // Kiểm tra mật khẩu với giá trị đã băm.
    public PasswordCheckResult VerifyHashedPassword(
        TAccount account,
        string passwordHash,
        string password)
        => passwordHasher.VerifyHashedPassword(account, passwordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheckResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
            _ => PasswordCheckResult.Failed
        };
}
