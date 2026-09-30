using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PetSpa.Modules.Customer.Endpoints.Requests;
using PetSpa.Modules.Customer.Infrastructure.Persistence;

namespace PetSpa.Modules.Customer.Application.Validators
{
    public class CreateCustomerRequestValidator: AbstractValidator<CreateCustomerRequest>
    {
        public CreateCustomerRequestValidator(CustomerDbContext db)
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ và tên không được để trống")
                .MaximumLength(100).WithMessage("Họ và tên tối đa 100 ký tự");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Số điện thoại không được để trống")
                .Matches(@"^(0[3|5|7|8|9])[0-9]{8}$").WithMessage("Số điện thoại không đúng định dạng")
                .MustAsync(async (phone, ct) =>
                {
                    var PhoneExist = await db.Customers.AnyAsync(t => t.Phone == phone.Trim(), ct);
                    return !PhoneExist;
                }).WithMessage("Số điện thoại này đã được đăng ký trong hệ thống");
            When(x => !string.IsNullOrWhiteSpace(x.Email), () => 
            {
                RuleFor(x => x.Email)
                    .EmailAddress().WithMessage("Địa chỉ email không đúng định dạng")
                    .MaximumLength(256)
                    .MustAsync(async (email, ct) =>
                    {
                        var EmailExist = await db.Customers.AnyAsync(t => t.Email == email.Trim(), ct);
                        return !EmailExist;
                    }).WithMessage("Email này đã được đăng ký trong hệ thống");
            });
            When(x => x.Gender.HasValue, () => 
            {
                RuleFor(x => x.Gender)
                    .IsInEnum().WithMessage("Giới tính không hợp lệ (chỉ chấp nhận Male, Female, Other)");
            });
            When(x => x.DateOfBirth.HasValue, () =>
            {
                RuleFor(x => x.DateOfBirth)
                    .LessThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                    .WithMessage("Ngày sinh không thể lớn hơn ngày hiện tại");
            });
        }
    }
}