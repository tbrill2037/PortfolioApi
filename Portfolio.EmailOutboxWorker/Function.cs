using System.Diagnostics;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Interfaces.Execution;
using Portfolio.Application.Common.Logging;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Utilities;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Portfolio.EmailOutboxWorker;

/// <summary>
/// AWS Lambda function responsible for processing pending email outbox records
/// and publishing them to the email queue.
/// </summary>
public sealed class Function
{
    private readonly Lazy<Task<IServiceProvider>> _serviceProvider;

    public Function()
    {
        _serviceProvider = new Lazy<Task<IServiceProvider>>(Startup.ConfigureAsync);
    }

    /// <summary>
    /// Processes pending email outbox records and publishes them to the email queue.
    /// </summary>
    public async Task FunctionHandler(System.Text.Json.JsonElement request, ILambdaContext context)
    {
        var functionStopwatch = Stopwatch.StartNew();

        var isSqsEvent =
            request.TryGetProperty("Records", out var records)
            && records.ValueKind == System.Text.Json.JsonValueKind.Array
            && records.GetArrayLength() > 0;

        var triggerType = isSqsEvent ? "SQS" : "Scheduled";

        var serviceProvider = await _serviceProvider.Value;

        var sqsOptions = serviceProvider.GetRequiredService<IOptions<SqsOptions>>().Value;
        var lambdaOptions = serviceProvider.GetRequiredService<IOptions<LambdaOptions>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<Function>>();
        var timeoutProvider = serviceProvider.GetRequiredService<IExecutionTimeoutProvider>();

        var queueName = AwsResourceParser.GetQueueNameFromUrl(sqsOptions.OutboxQueueUrl);
        var buffer = lambdaOptions.EmailOutboxProcessorBuffer;

        using var invocationScope = logger.BeginScope(new Dictionary<string, object?>
        {
            [LoggingProperties.Worker] = WorkerNames.EmailOutboxWorker,
            [LoggingProperties.FunctionName] = context.FunctionName,
            [LoggingProperties.QueueName] = queueName,
            [LoggingProperties.QueueType] = QueueTypes.EmailOutbox,
            [LoggingProperties.ProviderRequestId] = context.AwsRequestId,
            [LoggingProperties.TriggerType] = triggerType,
        });

        logger.LogInformation("{EventName} | Email outbox processing started.", EmailLogEvents.Worker.ProcessingStarted);

        try
        {
            using var timeout = timeoutProvider.CreateTimeout(
                context.RemainingTime,
                buffer);

            if (timeout is null)
            {
                logger.LogWarning(
                    "{EventName} | Not enough Lambda execution time remaining. RemainingMs={RemainingMs}, BufferMs={BufferMs}",
                    EmailLogEvents.Worker.InsufficientRemainingTime,
                    context.RemainingTime.TotalMilliseconds,
                    buffer.TotalMilliseconds);

                return;
            }

            using var scope = serviceProvider.CreateScope();

            var processor = scope.ServiceProvider.GetRequiredService<IEmailOutboxProcessor>();

            var result = await processor.ProcessAsync(timeout.Token);

            logger.LogInformation(
                "{EventName} | Email outbox processing completed. Retrieved={Retrieved}, Succeeded={Succeeded}, Failed={Failed}",
                EmailLogEvents.Worker.ProcessingSucceeded,
                result.Retrieved,
                result.Succeeded,
                result.Failed);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(
                ex,
                "{EventName} | Email outbox processing cancelled.",
                EmailLogEvents.Worker.ProcessingCancelled);

            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "{EventName} | Email outbox processing failed.",
                EmailLogEvents.Worker.ProcessingFailed);

            throw;
        }
        finally
        {
            logger.LogInformation(
                "{EventName} | Email outbox execution completed. DurationMs={DurationMs}",
                EmailLogEvents.Worker.ExecutionCompleted,
                functionStopwatch.ElapsedMilliseconds);
        }
    }
}