using FluentValidation;

namespace Portfolio.Application.Commands.User.Update.UpdateUserRoles;

/// <summary>
/// Validates user role update requests by ensuring a username and roles are provided.
/// </summary>
public sealed class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
{
    public UpdateUserRolesCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required.");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role is required.");
    }
}