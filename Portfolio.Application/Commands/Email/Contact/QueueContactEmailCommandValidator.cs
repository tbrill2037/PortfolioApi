using FluentValidation;

namespace Portfolio.Application.Commands.Email.Contact;

/// <summary>
/// Validates Contact Email requests before they are queued for processing.
/// </summary>
public sealed class QueueContactEmailCommandValidator : AbstractValidator<QueueContactEmailCommand>
{
    public QueueContactEmailCommandValidator()
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