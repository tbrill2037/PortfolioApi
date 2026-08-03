using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Infra.Data;

namespace Portfolio.Infra.Repository.Email;

/// <summary>
/// Provides persistence operations for email cooldown tracking.
/// </summary>
public sealed class EmailCooldownRepository : IEmailCooldownRepository
{
    private readonly ApplicationDbContext _context;

    public EmailCooldownRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<EmailRequestEntity?> GetLastRequestAsync(EmailCooldownContext context, CancellationToken cancellationToken)
    {
        return _context.EmailRequests
            .Where(x =>
                x.EmailAddress == context.EmailAddress &&
                x.EmailType == context.EmailType.ToString())
            .OrderByDescending(x => x.RequestedUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(EmailRequestEntity request, CancellationToken cancellationToken)
    {
        await _context.EmailRequests.AddAsync(request, cancellationToken);
    }
}