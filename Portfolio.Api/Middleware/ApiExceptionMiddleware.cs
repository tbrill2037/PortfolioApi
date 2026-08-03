using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Common.Exceptions;
using Portfolio.Application.Common.Exceptions.Email;
using Portfolio.Application.Common.Responses;
using System.Text.Json;


/// Middleware responsible for handling unhandled API exceptions and converting them
/// into consistent HTTP error responses.
/// </summary>
public sealed class ApiExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionMiddleware> _logger;

    public ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var traceId = context.TraceIdentifier;
        var statusCode = StatusCodes.Status500InternalServerError;
        var errorCode = ErrorCodes.Unexpected;
        var message = "An unexpected error occurred.";
        IDictionary<string, string[]>? errors = null;

        _logger.LogError(exception, "Unhandled API exception. TraceId={TraceId}", traceId);

        switch (exception)
        {
            case ValidationException validation:
                statusCode = StatusCodes.Status400BadRequest;
                errorCode = ErrorCodes.Validation;
                message = validation.Message;
                errors = validation.Errors;

                break;

            case NotFoundException notFound:
                statusCode = StatusCodes.Status404NotFound;
                errorCode = ErrorCodes.NotFound;
                message = notFound.Message;

                break;

            case UnauthorizedAccessException unauthorized:
                statusCode = StatusCodes.Status401Unauthorized;
                errorCode = ErrorCodes.Unauthorized;
                message = unauthorized.Message;

                break;

            case DbUpdateException:
                statusCode = StatusCodes.Status500InternalServerError;
                errorCode = ErrorCodes.Database;
                message = "A database error occurred.";

                break;

            case BadRequestException badRequest:
                statusCode = StatusCodes.Status400BadRequest;
                errorCode = ErrorCodes.BadRequest;
                message = badRequest.Message;

                break;

            case ForbiddenException forbidden:
                statusCode = StatusCodes.Status403Forbidden;
                errorCode = ErrorCodes.Forbidden;
                message = forbidden.Message;

                break;

            case EmailException email:
                statusCode = StatusCodes.Status500InternalServerError;
                errorCode = ErrorCodes.Email;
                message = email.Message;

                break;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponseFactory.Failure(
            message,
            errorCode,
            errors,
            traceId);

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}