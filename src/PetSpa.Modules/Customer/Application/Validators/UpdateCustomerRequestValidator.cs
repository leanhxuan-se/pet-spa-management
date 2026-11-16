// Định nghĩa quy tắc kiểm tra dữ liệu UpdateCustomerRequestValidator.
using FluentValidation;
using Microsoft.Extensions.Options;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Application.Validators
{
    public class UpdateCustomerRequestValidator: AbstractValidator<UpdateCustomerRequest>
    {
        // Thiết lập quy tắc kiểm tra dữ liệu đầu vào.
        public UpdateCustomerRequestValidator() {

            // 1. Kiểm tra dữ liệu cập nhật.
            RuleFor(x => x)
                .Must(HaveAtLeastOneProperty)
                .WithMessage("Cần cung cấp ít nhất một thông tin để cập nhật.");

            // 2. Kiểm tra mật khẩu.
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .When(x => x.Password is not null);

            // 3. Kiểm tra họ tên.
            When(x => x.FullName is not null, () =>
            {
                RuleFor(x => x.FullName!)
                    .NotEmpty().WithMessage("Họ và tên không được để trống.")
                    .MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");
            });

            // 4. Kiểm tra số điện thoại.
            When(x => x.Phone is not null, () =>
            {
                RuleFor(x => x.Phone!)
                    .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                    .Matches(@"^(0|\+84)[35789][0-9]{8}$").WithMessage("Số điện thoại không hợp lệ (định dạng số điện thoại Việt Nam).");
            });

            // 5. Kiểm tra giới tính.
            When(x => x.Gender.HasValue, () =>
            {
                RuleFor(x => x.Gender!.Value)
                    .IsInEnum().WithMessage("Giới tính không hợp lệ (chỉ chấp nhận Male, Female, Other).");
            });

            // 6. Kiểm tra ngày sinh.
            When(x => x.DateOfBirth.HasValue, () =>
            {
                RuleFor(x => x.DateOfBirth!.Value)
                    .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                    .WithMessage("Ngày sinh không thể lớn hơn ngày hiện tại.");
            });

            // 7. Kiểm tra email.
            When(x => !string.IsNullOrWhiteSpace(x.Email), () =>
            {
                RuleFor(x => x.Email!)
                    .EmailAddress().WithMessage("Địa chỉ email không đúng định dạng.")
                    .MaximumLength(150).WithMessage("Email không được vượt quá 150 ký tự.");
            });

            // 8. Kiểm tra đường dẫn ảnh.
            When(x => !string.IsNullOrWhiteSpace(x.AvtURL), () =>
            {
                RuleFor(x => x.AvtURL!)
                    .MaximumLength(2048).WithMessage("Đường dẫn ảnh đại diện không được vượt quá 2048 ký tự.");
            });

            // 9. Kiểm tra ghi chú.
            When(x => !string.IsNullOrWhiteSpace(x.Note), () =>
            {
                RuleFor(x => x.Note!)
                    .MaximumLength(1024).WithMessage("Ghi chú không được vượt quá 1024 ký tự.");
            });

            // 10. Kiểm tra trạng thái.
            When(x => x.Status.HasValue, () =>
            {
                RuleFor(x => x.Status!.Value)
                    .IsInEnum().WithMessage("Trạng thái khách hàng không hợp lệ.");
            });
        }

        // Kiểm tra có ít nhất một trường cần cập nhật.
        private static bool HaveAtLeastOneProperty(UpdateCustomerRequest req)
        {    //Check if at least one property is updated
            return req.FullName is not null
                || req.Gender.HasValue
                || req.DateOfBirth.HasValue
                || req.Email is not null
                || req.Phone is not null
                || req.AvtURL is not null
                || req.Note is not null
                || req.Password is not null
                || req.Status.HasValue;
        }
    }
}
