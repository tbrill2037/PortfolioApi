using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Logging;

namespace Portfolio.Infra.Services.Email;

/// <summary>
/// Monitors Amazon SES account status, quota availability, and production access.
/// </summary>
public sealed class SesQuotaMonitor : ISesQuotaMonitor
{
    private readonly IAmazonSimpleEmailServiceV2 _ses;
    private readonly ILogger<SesQuotaMonitor> _logger;

    public SesQuotaMonitor(IAmazonSimpleEmailServiceV2 ses, ILogger<SesQuotaMonitor> logger)
    {
        _ses = ses;
        _logger = logger;
    }

    /// <summary>
    /// Checks SES account configuration and reports quota status.
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
            _logger.LogError(
                ex,
                "{EventName} | Failed retrieving SES account information.",
                EmailLogEvents.Quota.MonitorCheckFailed);

            throw;
        }

        if (response.ProductionAccessEnabled != true)
            _logger.LogWarning("{EventName} | SES is still in sandbox mode.", EmailLogEvents.Quota.SandboxMode);

        var sendQuota = response.SendQuota;

        if (sendQuota is null)
        {
            _logger.LogError("{EventName} | SES quota information unavailable.", EmailLogEvents.Quota.QuotaUnavailable);

            return;
        }

        var remaining = sendQuota.Max24HourSend - sendQuota.SentLast24Hours;

        _logger.LogInformation(
            "{EventName} | SES quota status. Remaining={Remaining}, SentLast24Hours={SentLast24Hours}, Max24HourSend={Max24HourSend}, MaxSendRate={MaxSendRate}",
            EmailLogEvents.Quota.StatusChecked,
            remaining,
            sendQuota.SentLast24Hours,
            sendQuota.Max24HourSend,
            sendQuota.MaxSendRate);

        if (remaining < 100)
            _logger.LogWarning(
                "{EventName} | SES daily quota is almost exhausted. Remaining={Remaining}",
                EmailLogEvents.Quota.QuotaLow,
                remaining);
    }
}