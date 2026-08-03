namespace Portfolio.Application.Common.Responses;

/// <summary>
/// Provides factory methods for creating standardized API responses.
/// </summary>
public static class ApiResponseFactory
{
    public static ApiResponse Success(string message, string? traceId = null)
    {
        return ApiResponse.SuccessResponse(message, traceId);
    }

    public static ApiResponse<T> Success<T>(T data, string message, string? traceId = null)
    {
        return ApiResponse<T>.SuccessResponse(data, message, traceId);
    }

    public static ApiResponse Failure(
        string message,
        string errorCode,
        IDictionary<string, string[]>? errors = null,
        string? traceId = null)
    {
        return ApiResponse.FailureResponse(
            message,
            errorCode,
            errors,
            traceId);
    }
}