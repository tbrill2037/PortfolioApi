using Portfolio.Domain.Enums.Email;

namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents the result of an email processing operation.
/// </summary>
public sealed record EmailProcessResultModel
{
    public required EmailProcessResult Result { get; init; }
    public Guid? CorrelationId { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? ErrorMessage { get; init; }
}