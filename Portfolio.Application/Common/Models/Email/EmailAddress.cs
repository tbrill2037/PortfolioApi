namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents an email address with an optional display name.
/// </summary>
public sealed record EmailAddress
{
    public required string Address { get; init; }

    public required string DisplayName { get; init; }
}