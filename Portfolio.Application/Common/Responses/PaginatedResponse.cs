namespace Portfolio.Application.Common.Responses;

/// <summary>
/// Represents a paginated collection of items with associated pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items contained within the paged result.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}