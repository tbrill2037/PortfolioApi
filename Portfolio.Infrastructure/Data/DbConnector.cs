using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Portfolio.Infra.Data;

/// <summary>
/// Provides database connection creation using application configuration settings.
/// </summary>
public class DbConnector
{
    private readonly IConfiguration _configuration;

    public DbConnector(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Creates a SQL Server database connection using the configured connection string.
    /// </summary>
    public IDbConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
        }

        return new SqlConnection(connectionString);
    }
}