// Kiểm tra dữ liệu đăng nhập dùng chung cho khách hàng và nhân viên.
using FluentValidation;

namespace PetSpa.Modules.Auth.Endpoints;

internal sealed record LoginRequest(string Phone, string Password);

internal sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(20);
        RuleFor(request => request.Password).NotEmpty();
    }
}
