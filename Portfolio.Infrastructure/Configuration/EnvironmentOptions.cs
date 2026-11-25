namespace Portfolio.Infra.Configuration;

public sealed class EnvironmentOptions
{
    public const string SectionName = "Environment";

    public bool IsDevelopment { get; set; }
}