using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.AWS;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Interfaces.Lambda;
using Portfolio.Application.Common.Logging;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Application.Common.Serialization;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Utilities;
using System.Diagnostics;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Portfolio.EmailWorker;

/// <summary>
/// AWS Lambda function responsible for processing email dispatch messages from the SQS queue.
/// </summary>
public sealed class Function
{
    private readonly Lazy<Task<IServiceProvider>> _serviceProvider;

    public Function()
    {
        _serviceProvider = new Lazy<Task<IServiceProvider>>(Startup.ConfigureAsync);
    }

    /// <summary>
    /// Processes email dispatch messages received from SQS.
    /// </summary>
    /// <param name="sqsEvent">The SQS event containing email messages.</param>
    /// <param name="context">The Lambda execution context.</param>
    /// <returns>The batch processing result.</returns>
    public async Task<SQSBatchResponse> FunctionHandler(SQSEvent sqsEvent, ILambdaContext context)
    {
        var serviceProvider = await _serviceProvider.Value;

        var sqsOptions = serviceProvider.GetRequiredService<IOptions<SqsOptions>>().Value;
        var lambdaOptions = serviceProvider.GetRequiredService<IOptions<LambdaOptions>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<Function>>();
        var timeoutProvider = serviceProvider.GetRequiredService<ILambdaExecutionTimeoutProvider>();

        var queueName = AwsResourceParser.GetQueueNameFromUrl(sqsOptions.EmailQueueUrl);
        var buffer = lambdaOptions.EmailProcessorBuffer;

        using var invocationScope = logger.BeginScope(new Dictionary<string, object?>
        {
            [LoggingProperties.Worker] = WorkerNames.EmailWorker,
            [LoggingProperties.FunctionName] = context.FunctionName,
            [LoggingProperties.QueueName] = queueName,
            [LoggingProperties.QueueType] = QueueTypes.EmailDispatch,
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

                var email = JsonSerializer.Deserialize<EmailMessage>(record.Body, JsonSerializerOptionsDefaults.Default);

                if (email is null)
                {
                    logger.LogError(
                        "{EventName} | Email message was null.",
                        EmailLogEvents.Worker.MissingPayload);

                    SqsBatchFailureHelper.AddFailure(failures, record);

                    continue;
                }

                using var emailScope = logger.BeginScope(new Dictionary<string, object?>
                {
                    [LoggingProperties.CorrelationId] = email.CorrelationId,
                    [LoggingProperties.CampaignId] = email.CampaignId,
                    [LoggingProperties.EmailId] = email.Id,
                    [LoggingProperties.EmailTemplateType] = email.TemplateType,
                });

                logger.LogInformation(
                    "{EventName} | Email message received.",
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

                var processor = scope.ServiceProvider.GetRequiredService<IEmailProcessor>();

                var result = await processor.ProcessAsync(email, receiveCount, timeout.Token);

                HandleProcessingResult(result, email, record, failures, logger);
            }
            catch (JsonException ex)
            {
                logger.LogError(
                    ex,
                    "{EventName} | Invalid email JSON.",
                    EmailLogEvents.Worker.InvalidPayload);

                SqsBatchFailureHelper.AddFailure(failures, record);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(
                    ex,
                    "{EventName} | Email processing cancelled.",
                    EmailLogEvents.Worker.ProcessingCancelled);

                SqsBatchFailureHelper.AddFailure(failures, record);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "{EventName} | Unhandled email processing failure.",
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
            "{EventName} | Batch processing completed. Records={RecordCount}, Failures={FailureCount}, DurationMs={DurationMs}",
            EmailLogEvents.Worker.BatchCompleted,
            sqsEvent.Records.Count,
            failures.Count,
            stopwatch.ElapsedMilliseconds);

        return new SQSBatchResponse(failures);
    }

    private static void HandleProcessingResult(
        EmailProcessResultModel result,
        EmailMessage email,
        SQSEvent.SQSMessage record,
        List<SQSBatchResponse.BatchItemFailure> failures,
        ILogger logger)
    {
        switch (result.Result)
        {
            case EmailProcessResult.Sent:
                {
                    if (!string.IsNullOrWhiteSpace(result.SesMessageId))
                    {
                        using var resultScope = logger.BeginScope(new Dictionary<string, object>
                        {
                            [LoggingProperties.SesMessageId] = result.SesMessageId,
                        });

                        logger.LogInformation(
                            "{EventName} | SES accepted email.",
                            EmailLogEvents.Worker.SesAccepted);
                    }
                    else
                    {
                        logger.LogInformation(
                            "{EventName} | SES accepted email but returned no SES message ID.",
                            EmailLogEvents.Worker.SesAccepted);
                    }

                    break;
                }

            case EmailProcessResult.AlreadyProcessed:
                {
                    logger.LogInformation(
                        "{EventName} | Email was already processed. EmailId={EmailId}",
                        EmailLogEvents.Worker.AlreadyProcessed,
                        email.Id);

                    break;
                }

            case EmailProcessResult.Locked:
                {
                    logger.LogWarning(
                        "{EventName} | Email is locked. Returning message to SQS.",
                        EmailLogEvents.Worker.Locked);

                    SqsBatchFailureHelper.AddFailure(failures, record);

                    break;
                }

            case EmailProcessResult.RetryableFailure:
                {
                    logger.LogWarning(
                        "{EventName} | Email failed temporarily. Retrying.",
                        EmailLogEvents.Worker.TemporarilyFailed);

                    SqsBatchFailureHelper.AddFailure(failures, record);

                    break;
                }

            case EmailProcessResult.PermanentFailure:
                {
                    logger.LogError(
                        "{EventName} | Permanent email failure recorded. Message will not be retried.",
                        EmailLogEvents.Worker.ProcessingFailed);

                    break;
                }

            case EmailProcessResult.NotFound:
                {
                    logger.LogWarning(
                        "{EventName} | Removing stale SQS message because email record no longer exists. EmailId={EmailId}",
                        EmailLogEvents.Worker.StaleMessageDiscarded,
                        email.Id);

                    break;
                }

            default:
                {
                    logger.LogError(
                        "{EventName} | Unknown email processing result. Retrying.",
                        EmailLogEvents.Worker.UnknownProcessResult);

                    SqsBatchFailureHelper.AddFailure(failures, record);

                    break;
                }
        }
    }
}