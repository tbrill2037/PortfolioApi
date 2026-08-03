using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Parses SNS payloads into email events.
/// </summary>
public interface ISesEventParser
{
    /// <summary>
    /// Parses an SNS payload into an email event.
    /// </summary>
    /// <param name="snsPayload">The raw SNS payload.</param>
    /// <returns>The parsed email event.</returns>
    EmailEvent Parse(string snsPayload);
}