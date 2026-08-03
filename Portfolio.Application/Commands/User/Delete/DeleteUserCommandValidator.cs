using FluentValidation;

namespace Portfolio.Application.Commands.User.Delete;

/// <summary>
/// Validates user deletion requests by ensuring a user identifier is provided.
/// </summary>
public sealed class DeleteUserCommandValidator : AbstractValidator<DeleteUserCommand>
{
    public DeleteUserCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("User id is required.");
    }
}