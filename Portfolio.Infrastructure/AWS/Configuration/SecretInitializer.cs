using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Portfolio.Infra.AWS.Configuration;

/// <summary>
/// Loads AWS Secrets Manager values into application configuration.
/// </summary>
public static class SecretInitializer
{
    public static async Task LoadAsync(
        ConfigurationManager configuration,
        string? secretName)
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

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new InvalidOperationException(
                "AWS secret name is missing.");
        }

        using var client = new AmazonSecretsManagerClient();

        var response = await client.GetSecretValueAsync(
            new GetSecretValueRequest
            {
                SecretId = secretName
            });

        if (string.IsNullOrWhiteSpace(response.SecretString))
        {
            throw new InvalidOperationException(
                $"Secret '{secretName}' value is empty.");
        }

        using var stream =
            new MemoryStream(
                Encoding.UTF8.GetBytes(response.SecretString));

        configuration.AddJsonStream(stream);
    }
}