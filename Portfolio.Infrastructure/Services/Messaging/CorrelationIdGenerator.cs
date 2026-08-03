using Portfolio.Application.Common.Interfaces.Messaging;

namespace Portfolio.Infra.Services.Messaging;

/// <summary>
/// Generates unique identifiers used for request correlation and batch processing.
/// </summary>
public sealed class CorrelationIdGenerator : ICorrelationIdGenerator
{
    public Guid CreateCorrelationId()
    {
        return Guid.NewGuid();
    }

    public Guid CreateBatchId()
    {
        return Guid.NewGuid();
    }
}