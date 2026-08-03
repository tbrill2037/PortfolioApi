using Amazon.Lambda.Core;
using Portfolio.Application.Common.Models.Lambda;

namespace Portfolio.Application.Common.Interfaces.Lambda;

/// <summary>
/// Defines the contract for creating Lambda processing timeout information.
/// </summary>
public interface ILambdaExecutionTimeoutProvider
{
    ProcessingTimeoutResult? CreateTimeout(ILambdaContext context, TimeSpan buffer);
}