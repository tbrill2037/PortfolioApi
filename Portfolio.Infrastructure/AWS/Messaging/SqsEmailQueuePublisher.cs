using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Application.Common.Serialization;
using System.Text.Json;

namespace Portfolio.Infra.AWS.Messaging;

/// <summary>
/// Publishes email messages to an Amazon SQS queue.
/// </summary>
public sealed class SqsEmailQueuePublisher : IEmailQueuePublisher
{
    private readonly IAmazonSQS _sqs;
    private readonly SqsOptions _options;

    public SqsEmailQueuePublisher(
        IAmazonSQS sqs,
        IOptions<SqsOptions> options)
    {
        _sqs = sqs;
        _options = options.Value;
    }

    /// <summary>
    /// Publishes an email message to the configured SQS queue.
    /// </summary>
    public async Task<string> PublishAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        var response = await _sqs.SendMessageAsync(
            new SendMessageRequest
            {
                QueueUrl = _options.EmailQueueUrl,
                MessageBody = JsonSerializer.Serialize(
                    message,
                    JsonSerializerOptionsDefaults.Default)
            },
            cancellationToken);

        return response.MessageId;
    }
}