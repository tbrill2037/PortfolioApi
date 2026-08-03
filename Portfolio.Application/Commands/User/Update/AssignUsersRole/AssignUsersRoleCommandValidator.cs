using FluentValidation;

namespace Portfolio.Application.Commands.User.Update.AssignUsersRole;

/// <summary>
/// Validates role assignment requests by ensuring a user and roles are provided.
/// </summary>
public sealed class AssignUsersRoleCommandValidator : AbstractValidator<AssignUsersRoleCommand>
{
    public AssignUsersRoleCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role is required.");
    }
}