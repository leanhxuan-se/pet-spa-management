// Đọc và cập nhật thông tin đăng nhập còn lưu trong hồ sơ khách hàng.
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Domain.Enums;
using PetSpa.SharedKernel.Application.Authentication;

namespace PetSpa.Modules.Customer.Infrastructure.Persistence.Repositories;

internal sealed class CustomerLoginStore(CustomerDbContext db) : ILegacyLoginStore
{
    // Tìm tài khoản khách hàng đang hoạt động có mật khẩu theo số điện thoại.
    public Task<LegacyLoginAccount?> FindActiveByPhoneAsync(string phone, CancellationToken ct)
    {
        // Lọc hồ sơ hợp lệ và chuyển sang hợp đồng đăng nhập dùng chung.
        return db.Customers
            .Where(customer => customer.Phone == phone && customer.Status == CustomerStatus.ACTIVE &&
                customer.PasswordHash != null && customer.PasswordHash != "")
            .Select(customer => new LegacyLoginAccount(
                customer.Id, customer.FullName, customer.Phone, AccountRoles.Customer, customer.PasswordHash!))
            .SingleOrDefaultAsync(ct);
    }

    // Cập nhật mật khẩu băm và thời điểm đăng nhập nếu hồ sơ chưa thay đổi.
    public async Task<bool> RecordLoginAsync(
        LegacyLoginAccount account, string passwordHash, DateTime loggedAtUtc, CancellationToken ct)
    {
        // Đối chiếu hash cũ để lần đăng nhập không ghi đè mật khẩu vừa được đổi.
        var updated = await db.Customers
            .Where(customer => customer.Id == account.Id && customer.Phone == account.Phone &&
                customer.Status == CustomerStatus.ACTIVE && customer.PasswordHash == account.PasswordHash)
            .ExecuteUpdateAsync(update => update
                .SetProperty(customer => customer.PasswordHash, passwordHash)
                .SetProperty(customer => customer.LastLoginAt, loggedAtUtc), ct);

        // Chỉ xác nhận thành công khi đúng một hồ sơ được cập nhật.
        return updated == 1;
    }
}
