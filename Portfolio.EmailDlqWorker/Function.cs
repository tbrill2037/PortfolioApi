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
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Utilities;
using System.Diagnostics;
using System.Text.Json;

[assembly: LambdaSerializer(
    typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Portfolio.EmailDlqWorker;

/// <summary>
/// AWS Lambda function responsible for processing email messages from the dead-letter queue.
/// </summary>
public sealed class Function
{
    private readonly Lazy<Task<IServiceProvider>> _serviceProvider;

    public Function()
    {
        _serviceProvider = new Lazy<Task<IServiceProvider>>(
            Startup.ConfigureAsync);
    }

    /// <summary>
    /// Processes email messages received from the dead-letter queue.
    /// </summary>
    public async Task<SQSBatchResponse> FunctionHandler(
        SQSEvent sqsEvent,
        ILambdaContext context)
    {
        var serviceProvider = await _serviceProvider.Value;

        var sqsOptions = serviceProvider
            .GetRequiredService<IOptions<SqsOptions>>()
            .Value;

        var lambdaOptions = serviceProvider
            .GetRequiredService<IOptions<LambdaOptions>>()
            .Value;

        var logger = serviceProvider
            .GetRequiredService<ILogger<Function>>();

        var timeoutProvider = serviceProvider
            .GetRequiredService<ILambdaExecutionTimeoutProvider>();

        var queueName = AwsResourceParser.GetQueueNameFromUrl(
            sqsOptions.EmailDeadLetterQueueUrl);

        var buffer = lambdaOptions.DlqProcessorBuffer;

        using var invocationScope = logger.BeginScope(new Dictionary<string, object?>
        {
            [LoggingProperties.Worker] = WorkerNames.EmailDlqWorker,
            [LoggingProperties.FunctionName] = context.FunctionName,
            [LoggingProperties.QueueName] = queueName,
            [LoggingProperties.QueueType] = QueueTypes.EmailDeadLetter,
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
                using var scope = serviceProvider.CreateScope();

                var archiver = scope.ServiceProvider
                    .GetRequiredService<IEmailDeadLetterArchiver>();

                using var messageScope = logger.BeginScope(new Dictionary<string, object?>
                {
                    [LoggingProperties.MessageId] = record.MessageId,
                    [LoggingProperties.ReceiveCount] = receiveCount,
                    [LoggingProperties.SentTimestamp] = sentTimestamp,
                    [LoggingProperties.FirstReceiveTimestamp] =
                        SqsMessageHelper.GetFirstReceiveTimestamp(record),
                    [LoggingProperties.MessageAge] = messageAge,
                });

                EmailMessage? email = null;

                try
                {
                    email = JsonSerializer.Deserialize<EmailMessage>(
                        record.Body,
                        JsonSerializerOptionsDefaults.Default);
                }
                catch (JsonException ex)
                {
                    logger.LogError(
                        ex,
                        "{EventName} | Invalid DLQ payload. Archiving raw message.",
                        EmailLogEvents.Worker.InvalidPayload);

                    await archiver.ArchiveAsync(
                        record,
                        null,
                        ex,
                        CancellationToken.None);

                    continue;
                }

                if (email is null)
                {
                    logger.LogError(
                        "{EventName} | DLQ payload was empty. Archiving raw message.",
                        EmailLogEvents.Worker.MissingPayload);

                    await archiver.ArchiveAsync(
                        record,
                        null,
                        null,
                        CancellationToken.None);

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
                    "{EventName} | DLQ message received. ReceiveCount={ReceiveCount}",
                    EmailLogEvents.Worker.DlqMessageReceived,
                    receiveCount);

                using var timeout = timeoutProvider.CreateTimeout(
                    context,
                    buffer);

                if (timeout is null)
                {
                    logger.LogWarning(
                        "{EventName} | Not enough Lambda execution time remaining.",
                        EmailLogEvents.Worker.InsufficientRemainingTime);

                    SqsBatchFailureHelper.AddFailure(
                        failures,
                        record);

                    continue;
                }

                logger.LogInformation(
                    "{EventName} | Processing timeout selected. Timeout={TimeoutSeconds}",
                    EmailLogEvents.Worker.ProcessingTimeoutSelected,
                    timeout.Timeout.TotalSeconds);

                await archiver.ArchiveAsync(
                    record,
                    email,
                    null,
                    timeout.Token);

                logger.LogInformation(
                    "{EventName} | DLQ message archived successfully. EmailId={EmailId}",
                    EmailLogEvents.Worker.DlqMessageCompleted,
                    email.Id);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogWarning(
                    ex,
                    "{EventName} | DLQ processing cancelled.",
                    EmailLogEvents.Worker.ProcessingCancelled);

                SqsBatchFailureHelper.AddFailure(
                    failures,
                    record);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "{EventName} | DLQ processing failed.",
                    EmailLogEvents.Worker.ProcessingFailed);

                SqsBatchFailureHelper.AddFailure(
                    failures,
                    record);
            }
            finally
            {
                logger.LogInformation(
                    "{EventName} | Message completed. DurationMs={DurationMs}",
                    EmailLogEvents.Worker.DlqProcessingCompleted,
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
}