namespace Portfolio.Application.Common.Logging;

/// <summary>
/// Defines standardized property names used for structured logging.
/// </summary>
public static class LoggingProperties
{
    public const string Operation = nameof(Operation);
    public const string EmailTemplateType = nameof(EmailTemplateType);
    public const string EmailEventType = nameof(EmailEventType);
    public const string EmailMessageId = nameof(EmailMessageId);
    public const string CorrelationId = nameof(CorrelationId);
    public const string EmailId = nameof(EmailId);
    public const string CampaignId = nameof(CampaignId);
    public const string ProviderMessageId = nameof(ProviderMessageId);
    public const string ProviderRequestId = nameof(ProviderRequestId);
    public const string Worker = nameof(Worker);
    public const string ReceiveCount = nameof(ReceiveCount);
    public const string BatchSize = nameof(BatchSize);
    public const string QueueName = nameof(QueueName);
    public const string QueueType = nameof(QueueType);
    public const string FunctionName = nameof(FunctionName);
    public const string SentTimestamp = nameof(SentTimestamp);
    public const string FirstReceiveTimestamp = nameof(FirstReceiveTimestamp);
    public const string MessageAge = nameof(MessageAge);
    public const string RequestId = nameof(RequestId);
    public const string OutboxId = nameof(OutboxId);
    public const string QueueMessageId = nameof(QueueMessageId);
    public const string ProviderReceivedUtc = nameof(ProviderReceivedUtc);
    public const string OutboxMessageId = nameof(OutboxMessageId);
    public const string TriggerType = nameof(TriggerType);
}