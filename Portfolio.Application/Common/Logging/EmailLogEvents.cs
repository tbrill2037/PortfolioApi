namespace Portfolio.Application.Common.Logging;

/// <summary>
/// Defines standardized logging event names for email processing workflows.
/// </summary>
public static class EmailLogEvents
{
    public static class Processor
    {
        public const string Completed = "Email.Completed";
        public const string CheckingIdempotency = "Email.CheckingIdempotency";
        public const string AlreadyProcessed = "Email.AlreadyProcessed";
        public const string AlreadyProcessing = "Email.AlreadyProcessing";
        public const string StaleMessageDiscarded = "Email.StaleMessageDiscarded";
        public const string LockAcquired = "Email.LockAcquired";
        public const string LockLost = "Email.LockLost";
        public const string NotFoundAfterLock = "Email.NotFoundAfterLock";
        public const string Sent = "Email.Sent";
        public const string TerminalState = "Email.TerminalState";
        public const string PermanentlyFailed = "Email.PermanentlyFailed";
        public const string RetryMarked = "Email.RetryMarked";
        public const string FailureStateUpdateFailed = "Email.FailureStateUpdateFailed";
        public const string RetryStateUpdateFailed = "Email.RetryStateUpdateFailed";
        public const string StatusTransitioned = "Email.StatusTransitioned";
        public const string StatusTransitionIgnored = "Email.StatusTransitionIgnored";
        public const string NotFound = "Email.NotFound";
    }

    public static class Provider
    {
        public const string EventEmailMissing = "Email.EventEmailMissing";
        public const string ProviderSendStarted = "Email.ProviderSendStarted";
        public const string EmailAccepted = "Email.EmailAccepted";
        public const string EmailRejected = "Email.EmailRejected";
        public const string SendCancelled = "Email.SendCancelled";
        public const string SendFailed = "Email.SendFailed";
        public const string EventProcessingStarted = "Email.EventProcessingStarted";
        public const string EventAlreadyProcessed = "Email.EventAlreadyProcessed";
        public const string EventFailed = "Email.EventFailed";
        public const string NotFound = "Email.NotFound";
        public const string UnknownEventType = "Email.UnknownEventType";
        public const string EventIgnored = "Email.EventIgnored";
        public const string EventProcessed = "Email.EventProcessed";
        public const string EventCancelled = "Email.EventCancelled";
        public const string EventDatabaseError = "Email.EventDatabaseError";
        public const string StatusUpdated = "Email.StatusUpdated";
        public const string EmailMimeCreated = "Email.EmailMimeCreated";
        public const string EmailMimeMissingBody = "Email.EmailMimeMissingBody";
    }

    public static class Worker
    {
        public const string QueueEmpty = "Email.QueueEmpty";
        public const string MessageReceived = "Email.MessageReceived";
        public const string ProcessingStarted = "Email.ProcessingStarted";
        public const string ProcessingCancelled = "Email.ProcessingCancelled";
        public const string ProcessingSucceeded = "Email.ProcessingSucceeded";
        public const string ProcessingTimeoutSelected = "Email.ProcessingTimeoutSelected";
        public const string ExecutionTimeChecked = "Email.ExecutionTimeChecked";
        public const string ExecutionRemainingTime = "Email.ExecutionRemainingTime";
        public const string InsufficientRemainingTime = "Email.InsufficientRemainingTime";
        public const string MaxRetryAttemptsExceeded = "Email.MaxRetryAttemptsExceeded";
        public const string ProviderAccepted = "Email.ProviderAccepted";
        public const string AlreadyProcessed = "Email.AlreadyProcessed";
        public const string UnknownProcessResult = "Email.UnknownProcessResult";
        public const string Locked = "Email.Locked";
        public const string Retrying = "Email.Retrying";
        public const string TemporarilyFailed = "Email.TemporarilyFailed";
        public const string ProcessingFailed = "Email.ProcessingFailed";
        public const string DlqProcessingCompleted = "Email.DlqProcessingCompleted";
        public const string DlqMessageCompleted = "Email.DlqMessageCompleted";
        public const string DlqMessageReceived = "Email.DlqMessageReceived";
        public const string DeadLettered = "Email.DeadLettered";
        public const string PayloadParsed = "Email.PayloadParsed";
        public const string InvalidMessage = "Email.InvalidMessage";
        public const string MissingPayload = "Email.MissingPayload";
        public const string InvalidPayload = "Email.InvalidPayload";
        public const string StaleMessageDiscarded = "Email.StaleMessageDiscarded";
        public const string MessageProcessingCompleted = "Email.MessageProcessingCompleted";
        public const string BatchCompleted = "Email.BatchCompleted";
        public const string ExecutionCompleted = "Email.OutboxExecutionCompleted";
    }

    public static class Dlq
    {
        public const string NotFound = "Email.DlqNotFound";
        public const string Received = "Email.Received";
        public const string AlreadyDeadLettered = "Email.DlqAlreadyDeadLettered";
        public const string DlqArchived = "Email.DlqArchived";
        public const string MessageReceived = "Email.DlqMessageReceived";
        public const string MessageProcessed = "Email.DlqMessageProcessed";
        public const string MessageFailed = "Email.DlqMessageFailed";
        public const string MessageDiscarded = "Email.DlqMessageDiscarded";
    }

    public static class Queue
    {
        public const string QueueRequested = "Email.QueueRequested";
        public const string PersistingEmail = "Email.PersistingEmail";
        public const string EmailPersisted = "Email.EmailPersisted";
        public const string PersistenceFailed = "Email.PersistenceFailed";
        public const string QueueingMessage = "Email.QueueingMessage";
        public const string MessageQueued = "Email.MessageQueued";
        public const string QueueFailed = "Email.QueueFailed";
    }

    public static class Quota
    {
        public const string QuotaUnavailable = "Email.QuotaUnavailable";
        public const string QuotaExceeded = "Email.QuotaExceeded";
        public const string QuotaLow = "Email.QuotaLow";
        public const string QuotaCheckFailed = "Email.QuotaCheckFailed";
        public const string StatusChecked = "Email.ProviderStatusChecked";
        public const string MonitorCheckFailed = "Email.ProviderQuotaMonitorCheckFailed";
        public const string SandboxMode = "Email.ProviderSandboxMode";
    }

    public static class Outbox
    {
        public const string LockLost = "Email.LockLost";
        public const string MissingPayload = "Email.OutboxMissingPayload";
        public const string PublishingStarted = "Email.OutboxPublishingStarted";
        public const string Published = "Email.OutboxMessagePublished";
        public const string PublishFailed = "Email.OutboxPublishingFailed";
        public const string OutboxMessagesClaimed = "Email.OutboxMessagesClaimed";
        public const string OutboxMarkedPublished = "Email.OutboxMarkedPublished";
    }
}