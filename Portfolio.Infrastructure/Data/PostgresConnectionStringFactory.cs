
using Npgsql;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Portfolio.Infra.Data.Configuration;

namespace Portfolio.Infra.Data;

public sealed class PostgresConnectionStringFactory
{
    private readonly DatabaseOptions _options;
    private readonly IConfiguration _configuration;

    public PostgresConnectionStringFactory(
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

        if (string.IsNullOrWhiteSpace(_options.UserId))
        {
            throw new InvalidOperationException(
                "Database username is missing.");
        }

        var password = _configuration["Database:Password"];

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Database password is missing.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = _options.Server,
            Database = _options.Database,
            Username = _options.UserId,
            Password = password,
            SslMode = SslMode.Require
        };

        return builder.ConnectionString;
    }
}
