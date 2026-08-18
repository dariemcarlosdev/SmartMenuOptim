using FluentValidation;

namespace SmartMenuOptim.Application.Features.Auth.Commands;

public sealed class SignInWithPasswordCommandValidator : AbstractValidator<SignInWithPasswordCommand>
{
    public SignInWithPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
