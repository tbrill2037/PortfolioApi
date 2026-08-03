using FluentValidation;

namespace Portfolio.Application.Commands.Auth;

/// <summary>
/// Validates authentication requests by enforcing username and password requirements
/// before credentials are processed.
/// </summary>
public sealed class AuthCommandValidator : AbstractValidator<AuthCommand>
{
    public AuthCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(32).WithMessage("Password must be less than or equal to 32 characters.")
            .Matches(@"\d").WithMessage("Password must contain a number.")
            .Matches(@"[\p{P}\p{S}]").WithMessage("Password must contain a special character.");
    }
}