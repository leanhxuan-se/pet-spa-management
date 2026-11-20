using FluentValidation;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Application.Validators;

public sealed class UpdateCustomerRequestValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerRequestValidator()
    {
        RuleFor(x => x).Must(HasChanges).WithMessage("Cần cung cấp ít nhất một thông tin để cập nhật.");
        RuleFor(x => x.FullName!.Trim())
            .NotEmpty().WithMessage("Họ và tên không được để trống.")
            .MaximumLength(CustomerContact.FullNameMaxLength).WithMessage("Họ và tên tối đa 150 ký tự.")
            .OverridePropertyName(nameof(UpdateCustomerRequest.FullName))
            .When(x => x.FullName is not null);
        RuleFor(x => x.Phone).Must(CustomerContact.IsValidPhone)
            .WithMessage("Số điện thoại không đúng định dạng số di động Việt Nam.")
            .When(x => x.Phone is not null);
        RuleFor(x => CustomerContact.NormalizeEmail(x.Email))
            .EmailAddress().WithMessage("Địa chỉ email không đúng định dạng.")
            .MaximumLength(CustomerContact.EmailMaxLength).WithMessage("Email tối đa 255 ký tự.")
            .OverridePropertyName(nameof(UpdateCustomerRequest.Email));
        RuleFor(x => x.Gender).IsInEnum().WithMessage("Giới tính không hợp lệ.");
        RuleFor(x => x.DateOfBirth).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Ngày sinh không thể lớn hơn ngày hiện tại.");
        RuleFor(x => x.AvtURL).MaximumLength(2048).WithMessage("Đường dẫn ảnh đại diện tối đa 2048 ký tự.");
        RuleFor(x => x.Note).MaximumLength(1024).WithMessage("Ghi chú tối đa 1024 ký tự.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái khách hàng không hợp lệ.");
    }

    private static bool HasChanges(UpdateCustomerRequest req)
        => req.FullName is not null || req.Gender is not null || req.DateOfBirth is not null || req.HasEmail ||
            req.Phone is not null || req.AvtURL is not null || req.Note is not null ||
            req.Status is not null;
}
