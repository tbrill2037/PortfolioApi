
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Portfolio.Infra.Data.Configuration;

namespace Portfolio.Infra.Data;

/// <summary>
/// Creates an <see cref="ApplicationDbContext"/> for Entity Framework Core design-time operations,
/// such as generating and applying database migrations.
/// </summary>
/// <remarks>
/// This factory allows EF Core tools to create the database context without starting the API
/// or initializing unrelated application services such as authentication.
/// The application's normal runtime continues to create the context through dependency injection.
/// </remarks>
public sealed class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var databaseOptions = new DatabaseOptions
        {
            Server = configuration["Database:Server"] ?? string.Empty,
            Database = configuration["Database:Database"] ?? string.Empty,
            UserId = configuration["Database:UserId"] ?? string.Empty
        };

        var connectionStringFactory = new PostgresConnectionStringFactory(
            Options.Create(databaseOptions),
            configuration);

        var optionsBuilder =
            new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(
            connectionStringFactory.Create());

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
