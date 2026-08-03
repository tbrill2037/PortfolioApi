namespace Portfolio.Application.Common.Models.Lambda;

/// <summary>
/// Represents the configured timeout information used during Lambda processing.
/// Owns the cancellation token source used to stop processing before Lambda termination.
/// </summary>
public sealed class ProcessingTimeoutResult : IDisposable
{
    private readonly CancellationTokenSource _source;

    /// <summary>
    /// Gets the configured processing timeout.
    /// </summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Gets the cancellation token associated with the processing timeout.
    /// </summary>
    public CancellationToken Token => _source.Token;

    public ProcessingTimeoutResult(TimeSpan timeout, CancellationTokenSource source)
    {
        Timeout = timeout;
        _source = source;
    }

    public void Dispose() => _source.Dispose();
}