using Amazon.Lambda.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces.Lambda;
using Portfolio.Application.Common.Models.Lambda;
using Portfolio.Infra.AWS.Configuration;

namespace Portfolio.Infra.Services.Lambda;

/// <summary>
/// Creates Lambda processing timeouts while accounting for execution buffers.
/// </summary>
public sealed class LambdaExecutionTimeoutProvider : ILambdaExecutionTimeoutProvider
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
    public ProcessingTimeoutResult? CreateTimeout(ILambdaContext context, TimeSpan buffer)
    {
        var environment = _environmentOptions.IsDevelopment ? "Development" : "Production";

        var remainingTime =
            environment.Equals(
                "Development",
                StringComparison.OrdinalIgnoreCase)
                ? _lambdaOptions.DevelopmentFallback
                : context.RemainingTime;

        if (remainingTime <= buffer)
            return null;

        var processingTimeout = remainingTime - buffer;

        return new ProcessingTimeoutResult(processingTimeout, new CancellationTokenSource(processingTimeout));
    }
}