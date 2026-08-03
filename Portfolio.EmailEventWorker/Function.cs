using System.Diagnostics;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Exceptions;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Interfaces.Lambda;
using Portfolio.Application.Common.Logging;
using Portfolio.Application.Common.AWS;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Utilities;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Portfolio.EmailEventWorker;

/// <summary>
/// AWS Lambda function responsible for processing email events received from the SQS queue.
/// </summary>
public sealed class Function
{
    private readonly Lazy<Task<IServiceProvider>> _serviceProvider;

    public Function()
    {
        _serviceProvider = new Lazy<Task<IServiceProvider>>(Startup.ConfigureAsync);
    }

    /// <summary>
    /// Processes SES email events received from the SQS queue.
    /// </summary>
    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var serviceProvider = await _serviceProvider.Value;

        var sqsOptions = serviceProvider.GetRequiredService<IOptions<SqsOptions>>().Value;
        var lambdaOptions = serviceProvider.GetRequiredService<IOptions<LambdaOptions>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<Function>>();
        var timeoutProvider = serviceProvider.GetRequiredService<ILambdaExecutionTimeoutProvider>();
        var sesEventParser = serviceProvider.GetRequiredService<ISesEventParser>();

        var queueName = AwsResourceParser.GetQueueNameFromUrl(sqsOptions.EmailEventQueueUrl);
        var buffer = lambdaOptions.EmailEventProcessorBuffer;

        using var invocationScope = logger.BeginScope(new Dictionary<string, object?>
        {
            [LoggingProperties.Worker] = WorkerNames.EmailEventWorker,
            [LoggingProperties.FunctionName] = context.FunctionName,
            [LoggingProperties.QueueName] = queueName,
            [LoggingProperties.QueueType] = QueueTypes.EmailEvents,
            [LoggingProperties.AwsRequestId] = context.AwsRequestId,
            [LoggingProperties.BatchSize] = sqsEvent?.Records?.Count ?? 0,
        });

        if (sqsEvent?.Records is null || sqsEvent.Records.Count == 0)
        {
            logger.LogInformation(
                "{EventName} | Queue empty.",
                EmailLogEvents.Worker.QueueEmpty);

            return new SQSBatchResponse([]);
        }

        var stopwatch = Stopwatch.StartNew();
        var failures = new List<SQSBatchResponse.BatchItemFailure>();

        foreach (var record in sqsEvent.Records)
        {
            var messageStopwatch = Stopwatch.StartNew();

            var receiveCount = SqsMessageHelper.GetReceiveCount(record);
            var sentTimestamp = SqsMessageHelper.GetSentTimestamp(record);

            TimeSpan? messageAge = sentTimestamp.HasValue
                ? DateTimeOffset.UtcNow - sentTimestamp.Value
                : null;

            try
            {
                using var messageScope = logger.BeginScope(new Dictionary<string, object?>
                {
                    [LoggingProperties.MessageId] = record.MessageId,
                    [LoggingProperties.ReceiveCount] = receiveCount,
                    [LoggingProperties.SentTimestamp] = sentTimestamp,
                    [LoggingProperties.FirstReceiveTimestamp] = SqsMessageHelper.GetFirstReceiveTimestamp(record),
                    [LoggingProperties.MessageAge] = messageAge,
                });

                var emailEvent = sesEventParser.Parse(record.Body);

                logger.LogInformation(
                    "{EventName} | SES event received.",
                    EmailLogEvents.Worker.MessageReceived);

                using var timeout = timeoutProvider.CreateTimeout(context, buffer);

                if (timeout is null)
                {
                    logger.LogWarning(
                        "{EventName} | Not enough Lambda execution time remaining.",
                        EmailLogEvents.Worker.InsufficientRemainingTime);

                    SqsBatchFailureHelper.AddFailure(failures, record);

                    continue;
                }

                logger.LogInformation(
                    "{EventName} | Processing timeout selected. Timeout={Timeout}",
                    EmailLogEvents.Worker.ProcessingTimeoutSelected,
                    timeout.Timeout.TotalSeconds);

                using var scope = serviceProvider.CreateScope();

                var processor = scope.ServiceProvider.GetRequiredService<IEmailEventProcessor>();

                await processor.ProcessAsync(emailEvent, timeout.Token);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(
                    ex,
                    "{EventName} | SES processing cancelled.",
                    EmailLogEvents.Worker.ProcessingCancelled);

                SqsBatchFailureHelper.AddFailure(failures, record);
            }
            catch (JsonException ex)
            {
                logger.LogError(
                    ex,
                    "{EventName} | Invalid SES JSON payload.",
                    EmailLogEvents.Worker.InvalidPayload);

                SqsBatchFailureHelper.AddFailure(failures, record);
            }
            catch (EmailNotFoundException ex)
            {
                logger.LogWarning(
                    ex,
                    "{EventName} | Email record missing. Discarding event.",
                    EmailLogEvents.Worker.StaleMessageDiscarded);

                // Event cannot be recovered through retry.
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "{EventName} | Unhandled SES processing failure.",
                    EmailLogEvents.Worker.ProcessingFailed);

                SqsBatchFailureHelper.AddFailure(failures, record);
            }
            finally
            {
                logger.LogInformation(
                    "{EventName} | Message processing completed. DurationMs={DurationMs}",
                    EmailLogEvents.Worker.MessageProcessingCompleted,
                    messageStopwatch.ElapsedMilliseconds);
            }
        }

        logger.LogInformation(
            "{EventName} | Batch completed. Records={Records} Failures={Failures} DurationMs={DurationMs}",
            EmailLogEvents.Worker.BatchCompleted,
            sqsEvent.Records.Count,
            failures.Count,
            stopwatch.ElapsedMilliseconds);

        return new SQSBatchResponse(failures);
    }
}