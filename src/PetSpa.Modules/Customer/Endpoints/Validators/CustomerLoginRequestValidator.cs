using FluentValidation;
using PetSpa.Modules.Customer.Endpoints.Requests;

namespace PetSpa.Modules.Customer.Endpoints.Validators;

public class CustomerLoginRequestValidator
    : AbstractValidator<CustomerLoginRequest>
{
    public CustomerLoginRequestValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
