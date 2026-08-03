namespace Portfolio.Application.Common.Interfaces.Messaging;

/// <summary>
/// Generates correlation identifiers for messaging operations.
/// </summary>
public interface ICorrelationIdGenerator
{
    /// <summary>
    /// Creates a correlation identifier for a single operation.
    /// </summary>
    Guid CreateCorrelationId();

    /// <summary>
    /// Creates a correlation identifier for a batch operation.
    /// </summary>
    Guid CreateBatchId();
}