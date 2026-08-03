namespace Portfolio.Domain.Enums.Email;

/// <summary>
/// Provides validation rules for email lifecycle state transitions.
/// </summary>
public static class EmailStatusTransitionValidator
{
    /// <summary>
    /// Determines whether an email event is valid for the current email status.
    /// </summary>
    /// <param name="currentStatus">The current email lifecycle status.</param>
    /// <param name="eventType">The incoming email event.</param>
    /// <returns>
    /// <see langword="true"/> if the transition is allowed; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool CanTransition(
        EmailStatus currentStatus,
        EmailEventType eventType)
    {
        return eventType switch
        {
            EmailEventType.Unknown =>
                false,

            EmailEventType.Send =>
                currentStatus is EmailStatus.Pending
                    or EmailStatus.Processing
                    or EmailStatus.Retrying,

            EmailEventType.Delivery =>
                currentStatus is EmailStatus.Sent
                    or EmailStatus.Delivered,

            EmailEventType.Bounce =>
                currentStatus is EmailStatus.Sent
                    or EmailStatus.Delivered
                    or EmailStatus.Bounced,

            EmailEventType.Complaint =>
                currentStatus is EmailStatus.Sent
                    or EmailStatus.Complained,

            EmailEventType.Reject =>
                currentStatus is EmailStatus.Pending
                    or EmailStatus.Processing,

            EmailEventType.RenderingFailure =>
                currentStatus is EmailStatus.Sent,

            EmailEventType.Open =>
                currentStatus is EmailStatus.Delivered,

            EmailEventType.Click =>
                currentStatus is EmailStatus.Delivered,

            EmailEventType.DeliveryDelay =>
                currentStatus is EmailStatus.DeliveryDelay,

            EmailEventType.Subscription =>
                currentStatus is EmailStatus.Subscription,

            _ =>
                false
        };
    }
}