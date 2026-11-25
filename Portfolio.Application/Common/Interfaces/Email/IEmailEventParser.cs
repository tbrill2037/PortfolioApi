using Portfolio.Application.Common.Models.Email;

namespace Portfolio.Application.Common.Interfaces.Email;

/// <summary>
/// Parses provider event payloads into email events.
/// </summary>
public interface IEmailEventParser
{
    /// <summary>
    /// Parses a provider event payload into an email event.
    /// </summary>
    /// <param name="payload">The raw provider event payload.</param>
    /// <returns>The parsed email event.</returns>
    EmailEvent Parse(string payload);
}