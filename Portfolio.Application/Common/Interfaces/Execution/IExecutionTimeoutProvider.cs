using Portfolio.Application.Common.Models.Execution;

namespace Portfolio.Application.Common.Interfaces.Execution;

/// <summary>
/// Defines the contract for creating execution timeout information.
/// </summary>
public interface IExecutionTimeoutProvider
{
    ProcessingTimeoutResult? CreateTimeout(
        TimeSpan remainingTime,
        TimeSpan buffer);
}