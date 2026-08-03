namespace Portfolio.Domain.Enums.Email;

/// <summary>
/// Defines the lifecycle states of an email message during processing and delivery.
/// </summary>
public enum EmailStatus
{
    Pending = 1,

    Processing = 2,

    Retrying = 3,

    /// <summary>
    /// SES accepted the email request.
    /// </summary>
    Sent = 4,

    /// <summary>
    /// Permanent application failure; do not retry.
    /// </summary>
    Failed = 5,

    Rejected = 6,

    /// <summary>
    /// SES delivery event received.
    /// </summary>
    Delivered = 7,

    /// <summary>
    /// SES bounce event received.
    /// </summary>
    Bounced = 8,

    /// <summary>
    /// SES complaint event received.
    /// </summary>
    Complained = 9,

    /// <summary>
    /// SES open event received.
    /// </summary>
    Opened = 10,

    /// <summary>
    /// SES click event received.
    /// </summary>
    Clicked = 11,

    /// <summary>
    /// SES delivery delay event received.
    /// </summary>
    DeliveryDelay = 12,

    /// <summary>
    /// SES subscription event received.
    /// </summary>
    Subscription = 13,

    /// <summary>
    /// SQS retries exhausted.
    /// </summary>
    DeadLettered = 14
}