namespace Portfolio.Domain.Enums.Email;

/// <summary>
/// Defines the possible events that can occur during an email delivery lifecycle.
/// </summary>
public enum EmailEventType
{
    Unknown = 0,
    Send = 1,
    Delivery = 2,
    Bounce = 3,
    Complaint = 4,
    Reject = 5,
    RenderingFailure = 6,
    Open = 7,
    Click = 8,
    DeliveryDelay = 9,
    Subscription = 10,
}