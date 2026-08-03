namespace Portfolio.Infra.AWS.Configuration;

public sealed class EnvironmentOptions
{
    public const string SectionName = "Environment";

    public bool IsDevelopment { get; set; }
}