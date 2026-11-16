// Định nghĩa hợp đồng hoặc kết quả dùng chung: PasswordCheckResult.
namespace PetSpa.SharedKernel.Application.Abstractions;

public enum PasswordCheckResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}
