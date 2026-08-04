using Portfolio.Application.Common.Exceptions;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Entities.Email;
using Portfolio.Domain.Enums.Email;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Provides email request cooldown validation and tracking.
/// </summary>
public sealed class EmailCooldownService : IEmailCooldownService
{
    private readonly IEmailCooldownRepository _repository;

    public EmailCooldownService(IEmailCooldownRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// Checks whether an email request is allowed based on cooldown rules.
    /// </summary>
    public async Task CheckAsync(EmailCooldownContext context, CancellationToken cancellationToken)
    {
        var cooldown = GetCooldownPeriod(context.EmailType);

        if (cooldown == TimeSpan.Zero)
            return;

        var cutoff = DateTime.UtcNow - cooldown;
        var lastRequest = await _repository.GetLastRequestAsync(context, cancellationToken);

        if (lastRequest is not null && lastRequest.RequestedUtc > cutoff)
        {
            var elapsed = DateTime.UtcNow - lastRequest.RequestedUtc;
            var remaining = cooldown - elapsed;

            throw new EmailCooldownException(remaining);
        }

        await _repository.AddAsync(new EmailRequestEntity
        {
            Id = Guid.NewGuid(),
            UserId = context.UserId,
            EmailAddress = context.EmailAddress,
            IpAddress = context.IpAddress,
            EmailType = context.EmailType.ToString(),
            RequestedUtc = DateTime.UtcNow,
        }, cancellationToken);
    }

    private static TimeSpan GetCooldownPeriod(EmailType emailType)
    {
        return emailType switch
        {
            EmailType.Contact => TimeSpan.FromMinutes(5),
            EmailType.PasswordReset => TimeSpan.FromMinutes(5),
            EmailType.Verification => TimeSpan.FromMinutes(1),
            _ => TimeSpan.Zero,
        };
    }
}