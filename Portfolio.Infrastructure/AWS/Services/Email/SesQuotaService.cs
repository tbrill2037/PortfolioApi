using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Logging;
using Portfolio.Infra.AWS.Configuration;

namespace Portfolio.Infra.AWS.Services.Email;

/// <summary>
/// Validates Amazon SES sending quota availability before processing emails.
/// </summary>
public sealed class SesQuotaService : IEmailQuotaService
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly SesOptions _sesOptions;
    private readonly ILogger<SesQuotaService> _logger;

    public SesQuotaService(
        IAmazonSimpleEmailServiceV2 ses,
        IOptions<SesOptions> sesOptions,
        ILogger<SesQuotaService> logger)
    {
        _ses = ses;
        _sesOptions = sesOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Checks SES quota availability and prevents sending when the daily limit is exhausted.
    /// </summary>
    public async Task CheckAsync(CancellationToken cancellationToken)
    {
        GetAccountResponse response;

        try
        {
            response = await _ses.GetAccountAsync(new GetAccountRequest(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "{EventName} | Unable to retrieve SES quota information. Continuing email processing.",
                EmailLogEvents.Quota.QuotaCheckFailed);

            return;
        }

        var quota = response.SendQuota;

        if (quota is null)
        {
            _logger.LogWarning(
                "{EventName} | SES did not return quota information.",
                EmailLogEvents.Quota.QuotaUnavailable);

            return;
        }

        var remaining = quota.Max24HourSend - quota.SentLast24Hours;

        if (remaining <= 0)
        {
            _logger.LogError(
                "{EventName} | SES daily sending quota exhausted. Remaining={Remaining}, SentLast24Hours={SentLast24Hours}, Max24HourSend={Max24HourSend}",
                EmailLogEvents.Quota.QuotaExceeded,
                remaining,
                quota.SentLast24Hours,
                quota.Max24HourSend);

            throw new InvalidOperationException("SES daily sending quota exhausted.");
        }

        if (remaining < _sesOptions.LowQuotaThreshold)
        {
            _logger.LogWarning(
                "{EventName} | SES quota nearly exhausted. Remaining={Remaining}, SentLast24Hours={SentLast24Hours}, Max24HourSend={Max24HourSend}",
                EmailLogEvents.Quota.QuotaLow,
                remaining,
                quota.SentLast24Hours,
                quota.Max24HourSend);
        }
    }
}