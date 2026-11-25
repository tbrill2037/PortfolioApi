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
    /// The email provider accepted the email request.
    /// </summary>
    Sent = 4,

    /// <summary>
    /// Permanent application failure; do not retry.
    /// </summary>
    Failed = 5,

    Rejected = 6,

    /// <summary>
    /// Email delivery event received.
    /// </summary>
    Delivered = 7,

    /// <summary>
    /// Email bounce event received.
    /// </summary>
    Bounced = 8,

    /// <summary>
    /// Email complaint event received.
    /// </summary>
    Complained = 9,

    /// <summary>
    /// Email open event received.
    /// </summary>
    Opened = 10,

    /// <summary>
    /// Email click event received.
    /// </summary>
    Clicked = 11,

    /// <summary>
    /// Email delivery delay event received.
    /// </summary>
    DeliveryDelay = 12,

    /// <summary>
    /// Email subscription event received.
    /// </summary>
    Subscription = 13,

    /// <summary>
    /// Queue retries exhausted.
    /// </summary>
    DeadLettered = 14
}