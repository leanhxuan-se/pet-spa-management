using FluentValidation;
using Microsoft.Extensions.Options;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Application.Validators
{
    public class UpdatePetRequestValidator : AbstractValidator<UpdatePetRequest>
    {
        public UpdatePetRequestValidator()
        {

            RuleFor(x => x)
                .Must(HaveAtLeastOneProperty)
                .WithMessage("Cần cung cấp ít nhất một thông tin để cập nhật.");


            When(x => x.Name is not null, () =>
            {
                RuleFor(x => x.Name!)
                    .NotEmpty().WithMessage("Tên pet không được để trống.")
                    .MaximumLength(100).WithMessage("Tên pet không được vượt quá 100 ký tự.");
            });

            When(x => x.Species.HasValue, () =>
            {
                RuleFor(x => x.Species!.Value)
                    .IsInEnum().WithMessage("Loại pet không hợp lệ.");
            });

            When(x => x.Gender.HasValue, () =>
            {
                RuleFor(x => x.Gender!.Value)
                    .IsInEnum().WithMessage("Giới tính không hợp lệ (chỉ chấp nhận Male, Female, Other).");
            });

            When(x => x.DateOfBirth.HasValue, () =>
            {
                RuleFor(x => x.DateOfBirth!.Value)
                    .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                    .WithMessage("Ngày sinh không thể lớn hơn ngày hiện tại.");
            });

            When(x => x.Weight.HasValue, () =>
            {
                RuleFor(x => x.Weight!.Value)
                    .GreaterThan(0).WithMessage("Cân nặng phải là số dương.");
            });

            When(x => x.Height.HasValue, () =>
            {
                RuleFor(x => x.Height!.Value)
                    .GreaterThan(0).WithMessage("Chiều cao phải là số dương.");
            });

            When(x => !string.IsNullOrWhiteSpace(x.SpecialNote), () =>
            {
                RuleFor(x => x.SpecialNote!)
                    .MaximumLength(1024).WithMessage("Ghi chú không được vượt quá 1024 ký tự.");
            });

            When(x => x.Status.HasValue, () =>
            {
                RuleFor(x => x.Status!.Value)
                    .IsInEnum().WithMessage("Trạng thái khách hàng không hợp lệ.");
            });
        }

        private static bool HaveAtLeastOneProperty(UpdatePetRequest req)
        {    //Check if at least one property is updated
            return req.Name is not null
                || req.Gender.HasValue
                || req.Weight.HasValue
                || req.Height.HasValue
                || req.DateOfBirth.HasValue
                || req.Species is not null
                || req.SpecialNote is not null
                || req.Status.HasValue;
        }
    }
}
