using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces.Execution;
using Portfolio.Application.Common.Models.Execution;
using Portfolio.Infra.Configuration;
using Portfolio.Infra.AWS.Configuration;

namespace Portfolio.Infra.AWS.Services.Lambda;

/// <summary>
/// Creates Lambda processing timeouts while accounting for execution buffers.
/// </summary>
public sealed class LambdaExecutionTimeoutProvider : IExecutionTimeoutProvider
{
    private readonly LambdaOptions _lambdaOptions;
    private readonly EnvironmentOptions _environmentOptions;

    public LambdaExecutionTimeoutProvider(IOptions<LambdaOptions> lambdaOptions, IOptions<EnvironmentOptions> environmentOptions)
    {
        _lambdaOptions = lambdaOptions.Value;
        _environmentOptions = environmentOptions.Value;
    }

    /// <summary>
    /// Creates a cancellation timeout based on the remaining Lambda execution time.
    /// </summary>
    public ProcessingTimeoutResult? CreateTimeout(TimeSpan remainingTime, TimeSpan buffer)
    {
        if (_environmentOptions.IsDevelopment)
            remainingTime = _lambdaOptions.DevelopmentFallback;

        if (remainingTime <= buffer)
            return null;

        var processingTimeout = remainingTime - buffer;

        return new ProcessingTimeoutResult(processingTimeout, new CancellationTokenSource(processingTimeout));
    }
}