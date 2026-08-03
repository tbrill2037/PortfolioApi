namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Marker interface for strongly typed email template models.
/// 
/// Each email template should have its own model implementation.
/// This prevents templates from depending on API request models
/// and creates a clear boundary between application data and presentation.
/// </summary>
public interface IEmailTemplateModel
{
}