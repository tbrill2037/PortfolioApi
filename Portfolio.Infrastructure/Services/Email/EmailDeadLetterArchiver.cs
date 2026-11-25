using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Logging;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Archives failed email messages received from the dead letter queue.
/// </summary>
public sealed class EmailDeadLetterArchiver : IEmailDeadLetterArchiver
{
    private readonly IEmailDeadLetterRepository _deadLetterRepository;
    private readonly IEmailRepository _emailRepository;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EmailDeadLetterArchiver> _logger;

    public EmailDeadLetterArchiver(
        IEmailDeadLetterRepository deadLetterRepository,
        IEmailRepository emailRepository,
        ApplicationDbContext dbContext,
        ILogger<EmailDeadLetterArchiver> logger)
    {
        _deadLetterRepository = deadLetterRepository;
        _emailRepository = emailRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Archives a failed email message and updates its related email record.
    /// </summary>
    public async Task ArchiveAsync(
        EmailDeadLetterMessage message,
        EmailMessage? email,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            [LoggingProperties.Operation] = LoggingOperations.ProcessDeadLetter,
            [LoggingProperties.QueueMessageId] = message.QueueMessageId,
            [LoggingProperties.EmailId] = email?.Id,
        });

        var failureReason = exception?.Message
            ?? "Message moved to dead letter queue after retries were exhausted.";

        _logger.LogInformation(
            "{EventName} | Archiving DLQ email message.",
            EmailLogEvents.Dlq.Received);

        var entity = new EmailDeadLetterEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = email?.Id,
            QueueMessageId = message.QueueMessageId,
            Payload = message.Body,
            FailureReason = failureReason,
            ExceptionType = exception?.GetType().FullName,
            ReceiveCount = message.ReceiveCount,
            CreatedUtc = DateTime.UtcNow
        };

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            await _deadLetterRepository.AddAsync(entity, cancellationToken);

            if (email is not null)
            {
                await _emailRepository.MarkDeadLetteredAsync(
                    email.Id,
                    failureReason,
                    cancellationToken);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });

        _logger.LogWarning(
            "{EventName} | DLQ message archived. QueueMessageId={QueueMessageId}",
            EmailLogEvents.Dlq.DlqArchived,
            message.QueueMessageId);
    }
}