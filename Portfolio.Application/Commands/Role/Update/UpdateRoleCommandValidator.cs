using FluentValidation;

namespace Portfolio.Application.Commands.Role.Update;

/// <summary>
/// Validates role update requests by ensuring required role information is provided.
/// </summary>
public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role id is required.");

        RuleFor(x => x.RoleName)
            .NotEmpty().WithMessage("Role name is required.");
    }
}