using FluentValidation;
using MediatR;
using Portfolio.Application.Common.Exceptions;

namespace Portfolio.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior responsible for executing FluentValidation validators
/// before a request handler is executed.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Skip validation when no validators are registered.
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(result => result.Errors)
            .Where(error => error is not null)
            .ToList();

        if (failures.Count > 0)
        {
            throw new Portfolio.Application.Common.Exceptions.ValidationException(failures);
        }

        return await next();
    }
}