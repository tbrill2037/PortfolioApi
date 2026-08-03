namespace Portfolio.Application.Common.Responses;

/// <summary>
/// Represents a standardized API response without a data payload.
/// </summary>
public sealed class ApiResponse
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? ErrorCode { get; init; }

    public IDictionary<string, string[]>? Errors { get; init; }

    public string? TraceId { get; init; }

    public static ApiResponse SuccessResponse(string message = "Success.", string? traceId = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            TraceId = traceId
        };
    }

    public static ApiResponse FailureResponse(
        string message,
        string errorCode,
        IDictionary<string, string[]>? errors = null,
        string? traceId = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors,
            TraceId = traceId
        };
    }
}

/// <summary>
/// Represents a standardized API response containing a data payload.
/// </summary>
/// <typeparam name="T">The type of data returned in the response.</typeparam>
public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? ErrorCode { get; init; }

    public T? Data { get; init; }

    public IDictionary<string, string[]>? Errors { get; init; }

    public string? TraceId { get; init; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "Success.", string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            TraceId = traceId
        };
    }

    public static ApiResponse<T> FailureResponse(
        string message,
        string errorCode,
        IDictionary<string, string[]>? errors = null,
        string? traceId = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            Errors = errors,
            TraceId = traceId
        };
    }
}