using PetSpa.Modules.Customer.Application.Abstractions;
using PetSpa.Modules.Customer.Application.Dtos;
using PetSpa.Modules.Customer.Domain.Enums;
using PetSpa.Modules.Customer.Domain.Repositories;

namespace PetSpa.Modules.Customer.Application.Services;

public class CustomerLoginService(
    ICustomerRepository customers,
    IPasswordService passwordService)
{
    public async Task<CustomerLoginResult?> LoginAsync(
        string phone,
        string password,
        CancellationToken ct)
    {
        var normalizedPhone = phone.Trim();

        // 1. Tìm khách hàng theo số điện thoại.
        var customer = await customers.GetByPhoneAsync(normalizedPhone, ct);

        // 2. Kiểm tra tài khoản có được phép đăng nhập.
        if (customer is null ||
            customer.Status != CustomerStatus.ACTIVE ||
            string.IsNullOrEmpty(customer.PasswordHash))
        {
            return null;
        }

        // 3. So sánh mật khẩu nhập vào với hash đã lưu.
        var verification = passwordService.VerifyHashedPassword(
            customer,
            customer.PasswordHash,
            password);

        if (verification == PasswordCheckResult.Failed)
        {
            return null;
        }

        // 4. Nâng cấp hash nếu cấu hình băm đã thay đổi.
        if (verification == PasswordCheckResult.SuccessRehashNeeded)
        {
            customer.PasswordHash = passwordService.HashPassword(
                customer,
                password);
        }

        // 5. Lưu thời điểm đăng nhập thành công.
        customer.LastLoginAt = DateTime.UtcNow;
        await customers.SaveAsync(ct);

        return new CustomerLoginResult(
            customer.Id,
            customer.FullName,
            customer.Phone);
    }
}
