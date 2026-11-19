using FluentValidation;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Infrastructure.Persistence.Configurations;

namespace PetSpa.Modules.Customer.Application.Validators;

public sealed class CreatePetRequestValidator : AbstractValidator<CreatePetRequest>
{
    public CreatePetRequestValidator()
    {
        RuleFor(x => x.CustomersId).GreaterThan(0).WithMessage("ID khách hàng phải là số dương.");
        RuleFor(x => x.Name == null ? null : x.Name.Trim())
            .NotEmpty().WithMessage("Tên pet không được để trống.")
            .MaximumLength(PetConfiguration.NameMaxLength).WithMessage("Tên pet tối đa 100 ký tự.")
            .OverridePropertyName(nameof(CreatePetRequest.Name));
        RuleFor(x => x.Species)
            .NotNull().WithMessage("Cần cung cấp loài thú cưng.")
            .IsInEnum().WithMessage("Loài thú cưng không hợp lệ.");
        RuleFor(x => x.Gender).IsInEnum().WithMessage("Giới tính không hợp lệ.");
        RuleFor(x => x.DateOfBirth).LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Ngày sinh không thể lớn hơn ngày hiện tại.");
        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage("Cân nặng phải là số dương.")
            .PrecisionScale(6, 2, ignoreTrailingZeros: true).WithMessage("Cân nặng tối đa 4 chữ số nguyên và 2 chữ số thập phân.");
        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Chiều cao phải là số dương.")
            .PrecisionScale(6, 2, ignoreTrailingZeros: true).WithMessage("Chiều cao tối đa 4 chữ số nguyên và 2 chữ số thập phân.");
        RuleFor(x => x.SpecialNote).MaximumLength(1024).WithMessage("Ghi chú đặc biệt tối đa 1024 ký tự.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái thú cưng không hợp lệ.");
    }
}
