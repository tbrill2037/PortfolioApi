using FluentAssertions;
using Portfolio.Domain.Enums.Email;
using Xunit;

namespace Portfolio.UnitTests.Email;

public class EmailStatusTransitionTests
{
    [Theory]
    [InlineData(EmailStatus.Pending, EmailEventType.Send, true)]
    [InlineData(EmailStatus.Processing, EmailEventType.Send, true)]
    [InlineData(EmailStatus.Retrying, EmailEventType.Send, true)]
    [InlineData(EmailStatus.Delivered, EmailEventType.Send, false)]
    public void Send_ShouldOnlyAllowValidStatuses(
        EmailStatus status,
        EmailEventType eventType,
        bool expected)
    {
        var result =
            EmailStatusTransitionValidator.CanTransition(
                status,
                eventType);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(EmailStatus.Sent, EmailEventType.Delivery, true)]
    [InlineData(EmailStatus.Delivered, EmailEventType.Delivery, true)]
    [InlineData(EmailStatus.Pending, EmailEventType.Delivery, false)]
    public void Delivery_ShouldAllowOnlyValidStatuses(
        EmailStatus status,
        EmailEventType eventType,
        bool expected)
    {
        var result =
            EmailStatusTransitionValidator.CanTransition(
                status,
                eventType);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(EmailStatus.Sent, EmailEventType.Bounce, true)]
    [InlineData(EmailStatus.Delivered, EmailEventType.Bounce, true)]
    [InlineData(EmailStatus.Bounced, EmailEventType.Bounce, true)]
    [InlineData(EmailStatus.Pending, EmailEventType.Bounce, false)]
    public void Bounce_ShouldAllowDeliveryFailures(
        EmailStatus status,
        EmailEventType eventType,
        bool expected)
    {
        var result =
            EmailStatusTransitionValidator.CanTransition(
                status,
                eventType);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(EmailStatus.Sent, EmailEventType.Complaint, true)]
    [InlineData(EmailStatus.Complained, EmailEventType.Complaint, true)]
    [InlineData(EmailStatus.Pending, EmailEventType.Complaint, false)]
    public void Complaint_ShouldOnlyAllowValidStatuses(
        EmailStatus status,
        EmailEventType eventType,
        bool expected)
    {
        var result =
            EmailStatusTransitionValidator.CanTransition(
                status,
                eventType);

        result.Should().Be(expected);
    }

    [Fact]
    public void UnknownEvent_ShouldNeverTransition()
    {
        var result =
            EmailStatusTransitionValidator.CanTransition(
                EmailStatus.Sent,
                EmailEventType.Unknown);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(EmailStatus.Sent, EmailEventType.RenderingFailure, true)]
    [InlineData(EmailStatus.Delivered, EmailEventType.RenderingFailure, false)]
    public void RenderingFailure_ShouldOnlyAllowSent(
    EmailStatus status,
    EmailEventType eventType,
    bool expected)
    {
        var result =
            EmailStatusTransitionValidator.CanTransition(
                status,
                eventType);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(EmailStatus.Delivered, EmailEventType.Open, true)]
    [InlineData(EmailStatus.Sent, EmailEventType.Open, false)]
    public void Open_ShouldOnlyAllowDelivered(
        EmailStatus status,
        EmailEventType eventType,
        bool expected)
    {
        var result =
            EmailStatusTransitionValidator.CanTransition(
                status,
                eventType);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(EmailStatus.Delivered, EmailEventType.Click, true)]
    [InlineData(EmailStatus.Sent, EmailEventType.Click, false)]
    public void Click_ShouldOnlyAllowDelivered(
        EmailStatus status,
        EmailEventType eventType,
        bool expected)
    {
        var result =
            EmailStatusTransitionValidator.CanTransition(
                status,
                eventType);

        result.Should().Be(expected);
    }
}