using FluentValidation;

namespace Portfolio.Application.Commands.Role.Create;

/// <summary>
/// Validates role creation requests by ensuring required role information is provided.
/// </summary>
public sealed class RoleCreateCommandValidator : AbstractValidator<RoleCreateCommand>
{
    public RoleCreateCommandValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.");
    }
}