using FluentValidation;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Application.Validators;

internal sealed class CustomerLoginRequestValidator : AbstractValidator<CustomerLoginRequest>
{
    public CustomerLoginRequestValidator()
    {
        RuleFor(x => x.Phone).NotEmpty()
            .Must(x => x is null || CustomerContact.NormalizePhone(x).Length <= CustomerContact.PhoneMaxLength);
        RuleFor(x => x.Password).NotEmpty();
    }
}
