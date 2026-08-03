namespace Portfolio.Application.Common.Responses;

/// <summary>
/// Defines standardized error codes returned by the API.
/// </summary>
public static class ErrorCodes
{
    public const string Unexpected = "UNEXPECTED";
    public const string Database = "DATABASE_ERROR";
    public const string Email = "EMAIL_ERROR";
    public const string Validation = "VALIDATION_ERROR";
    public const string BadRequest = "BAD_REQUEST";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string Conflict = "CONFLICT";
    public const string Internal = "INTERNAL_ERROR";
    public const string EmailCooldown = "EMAIL_COOLDOWN";
    public const string EmailComposition = "EMAIL_COMPOSITION_ERROR";
    public const string EmailTemplateNotFound = "EMAIL_TEMPLATE_NOT_FOUND";
    public const string InvalidEmailTemplate = "INVALID_EMAIL_TEMPLATE";
    public const string InvalidTemplateModel = "INVALID_TEMPLATE_MODEL";
}