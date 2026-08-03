using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Repository.Email;

/// <summary>
/// Provides persistence operations for email dead letter records.
/// </summary>
public sealed class EmailDeadLetterRepository : IEmailDeadLetterRepository
{
    private readonly ApplicationDbContext _dbContext;

    public EmailDeadLetterRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(EmailDeadLetterEntity entity, CancellationToken cancellationToken)
    {
        await _dbContext.EmailDeadLetters.AddAsync(entity, cancellationToken);
    }
}