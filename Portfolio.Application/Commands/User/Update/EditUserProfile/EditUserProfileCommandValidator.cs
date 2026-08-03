using FluentValidation;

namespace Portfolio.Application.Commands.User.Update.EditUserProfile;

/// <summary>
/// Validates user profile update requests by ensuring required user information
/// and assigned roles are provided.
/// </summary>
public sealed class EditUserProfileCommandValidator : AbstractValidator<EditUserProfileCommand>
{
    public EditUserProfileCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User id is required.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email format is invalid.");

        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role is required.");
    }
}