using FluentValidation;
using PetSpa.SharedKernel.Application.Abstractions;

namespace PetSpa.SharedKernel.Application.Validators;

public sealed class LoginRequestValidator<TRequest> : AbstractValidator<TRequest>
    where TRequest : ILoginRequest
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(20);

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
