using FluentValidation.Results;
using Microsoft.AspNetCore.Identity;

namespace Portfolio.Application.Common.Exceptions;

/// <summary>
/// Represents validation failures from application or identity validation rules.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(x => x.PropertyName, x => x.ErrorMessage)
            .ToDictionary(x => x.Key, x => x.ToArray());
    }

    public ValidationException(IEnumerable<IdentityError> errors)
        : this()
    {
        Errors = errors
            .GroupBy(x => x.Code, x => x.Description)
            .ToDictionary(x => x.Key, x => x.ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}