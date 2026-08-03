namespace Portfolio.Infra.Data.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Database { get; set; } = string.Empty;

    public string Server { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}