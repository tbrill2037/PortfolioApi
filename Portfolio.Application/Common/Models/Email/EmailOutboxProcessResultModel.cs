namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents the result of an email outbox processing operation.
/// </summary>
public sealed record EmailOutboxProcessResultModel
{
    public int Retrieved { get; init; }

    public int Succeeded { get; init; }

    public int Failed { get; init; }
}