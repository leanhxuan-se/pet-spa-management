using FluentValidation;
using PetSpa.Modules.Customer.Domain.Enums;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Application.Validators;

internal sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.FullName == null ? null : x.FullName.Trim())
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(CustomerContact.FullNameMaxLength).WithMessage("Họ và tên tối đa 150 ký tự.")
            .OverridePropertyName(nameof(CreateCustomerRequest.FullName));
        RuleFor(x => x.Password).NotEmpty().WithMessage("Mật khẩu không được để trống.");
        RuleFor(x => x.Phone).Must(CustomerContact.IsValidPhone)
            .WithMessage("Số điện thoại không đúng định dạng số di động Việt Nam.");
        RuleFor(x => CustomerContact.NormalizeEmail(x.Email))
            .EmailAddress().WithMessage("Địa chỉ email không đúng định dạng.")
            .MaximumLength(CustomerContact.EmailMaxLength).WithMessage("Email tối đa 255 ký tự.")
            .OverridePropertyName(nameof(CreateCustomerRequest.Email));
        RuleFor(x => x.Gender).IsInEnum().WithMessage("Giới tính không hợp lệ.");
        RuleFor(x => x.DateOfBirth).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Ngày sinh không thể lớn hơn ngày hiện tại.");
        RuleFor(x => x.Status).Equal(CustomerStatus.ACTIVE).WithMessage("Khách hàng mới phải ở trạng thái ACTIVE.");
    }
}
