using FluentValidation;

namespace Portfolio.Application.Commands.Role.Delete;

/// <summary>
/// Validates role deletion requests by ensuring a role identifier is provided.
/// </summary>
public sealed class DeleteRoleCommandValidator : AbstractValidator<DeleteRoleCommand>
{
    public DeleteRoleCommandValidator()
    {
        RuleFor(x => x.RoleId)
            .NotEmpty().WithMessage("Role id is required.");
    }
}