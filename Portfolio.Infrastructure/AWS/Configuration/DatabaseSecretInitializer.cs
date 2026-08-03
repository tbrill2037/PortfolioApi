using Microsoft.Extensions.Configuration;

namespace Portfolio.Infra.AWS.Configuration;

public static class DatabaseSecretInitializer
{
    public static void Load(ConfigurationManager configuration)
    {
        var environment =
            configuration["ASPNETCORE_ENVIRONMENT"]
            ?? configuration["DOTNET_ENVIRONMENT"]
            ?? "Production";

        if (environment.Equals(
                "Development",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var username = configuration["username"];
        var password = configuration["password"];

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new InvalidOperationException(
                "Database username is missing from RDS secret.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Database password is missing from RDS secret.");
        }

        configuration["Database:UserId"] = username;
        configuration["Database:Password"] = password;
    }
}