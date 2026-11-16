// Định nghĩa hợp đồng hoặc kết quả dùng chung: IPasswordService.
namespace PetSpa.SharedKernel.Application.Abstractions;

public interface IPasswordService<TAccount> where TAccount : class
{
    // Băm mật khẩu trước khi lưu.
    string HashPassword(TAccount account, string password);

    // Kiểm tra mật khẩu với giá trị đã băm.
    PasswordCheckResult VerifyHashedPassword(
        TAccount account,
        string passwordHash,
        string password);
}
