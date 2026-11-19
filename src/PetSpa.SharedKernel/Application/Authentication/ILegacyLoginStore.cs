// Kết nối Auth với dữ liệu đăng nhập cũ trong thời gian chưa chuyển sang AuthDbContext.
namespace PetSpa.SharedKernel.Application.Authentication;

public interface ILegacyLoginStore
{
    Task<LegacyLoginAccount?> FindActiveByPhoneAsync(string phone, CancellationToken ct);

    // Chỉ ghi khi credential và quyền vẫn khớp với dữ liệu đã xác thực.
    Task<bool> RecordLoginAsync(LegacyLoginAccount account, string passwordHash, DateTime loggedAtUtc, CancellationToken ct);
}

public sealed class LegacyLoginAccount(long id, string fullName, string phone, string role, string passwordHash)
{
    public long Id { get; } = id;
    public string FullName { get; } = fullName;
    public string Phone { get; } = phone;
    public string Role { get; } = role;
    public string PasswordHash { get; } = passwordHash;
}
