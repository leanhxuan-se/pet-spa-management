// Định nghĩa quy tắc kiểm tra dữ liệu CreatePetRequestValidator.
using FluentValidation;
using Microsoft.Extensions.Options;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Application.Validators
{
    public class CreatePetRequestValidator : AbstractValidator<CreatePetRequest>
    {
        // Thiết lập quy tắc kiểm tra dữ liệu đầu vào.
        public CreatePetRequestValidator()
        {

         
            // 1. Kiểm tra tên.
            RuleFor(x => x.Name!)
                .NotEmpty().WithMessage("Tên pet không được để trống.")
                .MaximumLength(100).WithMessage("Tên pet không được vượt quá 100 ký tự.");

            // 2. Kiểm tra mã khách hàng.
            RuleFor(x => x.CustomersId)
                .GreaterThan(0).WithMessage("ID khách hàng phải là số dương.");

            // 3. Kiểm tra giới tính.
            When(x => x.Gender.HasValue, () =>
            {
                RuleFor(x => x.Gender!.Value)
                    .IsInEnum().WithMessage("Giới tính không hợp lệ (chỉ chấp nhận Male, Female, Other).");
            });

            // 4. Kiểm tra loài thú cưng.
            RuleFor(x => x.Species)
                .IsInEnum().WithMessage("Loại pet không hợp lệ.");

            // 5. Kiểm tra ngày sinh.
            When(x => x.DateOfBirth.HasValue, () =>
            {
                RuleFor(x => x.DateOfBirth!.Value)
                    .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                    .WithMessage("Ngày sinh không thể lớn hơn ngày hiện tại.");
            });

            // 6. Kiểm tra cân nặng.
            When(x => x.Weight.HasValue, () =>
            {
                RuleFor(x => x.Weight!.Value)
                    .GreaterThan(0).WithMessage("Cân nặng phải là số dương  .");
            });

            // 7. Kiểm tra chiều cao.
            When(x => x.Height.HasValue, () =>
            {
                RuleFor(x => x.Height!.Value)
                    .GreaterThan(0).WithMessage("Chiều cao phải là số dương.");
            });

            // 8. Kiểm tra ghi chú đặc biệt.
            When(x => !string.IsNullOrWhiteSpace(x.SpecialNote), () =>
            {
                RuleFor(x => x.SpecialNote!)
                    .MaximumLength(1024).WithMessage("Ghi chú không được vượt quá 1024 ký tự.");
            });

                RuleFor(x => x.Status)
                    .IsInEnum().WithMessage("Trạng thái khách hàng không hợp lệ.");


        }
    }
}
