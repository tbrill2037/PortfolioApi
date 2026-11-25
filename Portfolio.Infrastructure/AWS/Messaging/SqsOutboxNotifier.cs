using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Infra.AWS.Configuration;
using System.Text.Json;

namespace Portfolio.Infra.AWS.Messaging;

/// <summary>
/// Notifies the email outbox processor that new messages are available.
/// </summary>
public sealed class SqsOutboxNotifier : IEmailOutboxNotifier
{
    private readonly IAmazonSQS _sqs;
    private readonly SqsOptions _options;

    public SqsOutboxNotifier(
        IAmazonSQS sqs,
        IOptions<SqsOptions> options)
    {
        _sqs = sqs;
        _options = options.Value;
    }

    /// <summary>
    /// Sends a notification to the configured outbox queue.
    /// </summary>
    public async Task NotifyAsync(
        Guid emailId,
        CancellationToken cancellationToken = default)
    {
        var request = new SendMessageRequest
        {
            QueueUrl = _options.OutboxQueueUrl,
            MessageBody = JsonSerializer.Serialize(new
            {
                EmailId = emailId,
                EventType = "ProcessOutbox"
            })
        };

        await _sqs.SendMessageAsync(request, cancellationToken);
    }
}