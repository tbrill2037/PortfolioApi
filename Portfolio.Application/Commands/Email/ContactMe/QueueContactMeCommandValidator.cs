using FluentValidation;

namespace Portfolio.Application.Commands.Email.ContactMe;

/// <summary>
/// Validates Contact Me requests before they are queued for processing.
/// </summary>
public sealed class QueueContactMeCommandValidator : AbstractValidator<QueueContactMeCommand>
{
    public QueueContactMeCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(5000);
    }
}