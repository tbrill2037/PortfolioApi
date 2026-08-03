using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Portfolio.Infra.Data.Configuration;

namespace Portfolio.Infra.Data;

public sealed class SqlConnectionStringFactory
{
    private readonly DatabaseOptions _options;
    private readonly IConfiguration _configuration;

    public SqlConnectionStringFactory(
        IOptions<DatabaseOptions> options,
        IConfiguration configuration)
    {
        _options = options.Value;
        _configuration = configuration;
    }

    public string Create()
    {
        if (string.IsNullOrWhiteSpace(_options.Server))
        {
            throw new InvalidOperationException(
                "Database server is missing.");
        }

        if (string.IsNullOrWhiteSpace(_options.Database))
        {
            throw new InvalidOperationException(
                "Database name is missing.");
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = _options.Server,
            InitialCatalog = _options.Database,
            Encrypt = true,
            TrustServerCertificate = false
        };

        var environment =
            _configuration["ASPNETCORE_ENVIRONMENT"]
            ?? _configuration["DOTNET_ENVIRONMENT"]
            ?? "Production";

        if (environment.Equals(
                "Development",
                StringComparison.OrdinalIgnoreCase))
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            var password =
                _configuration["Database:Password"];

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Database password is missing.");
            }

            if (string.IsNullOrWhiteSpace(_options.UserId))
            {
                throw new InvalidOperationException(
                    "Database username is missing.");
            }

            builder.UserID = _options.UserId;
            builder.Password = password;
        }

        return builder.ConnectionString;
    }
}