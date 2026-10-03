using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace OrderBook.Api.Persistence;

/// <summary>
/// Resolves the audit database file and initializes its schema at startup.
/// Runtime and EF tooling use this same configuration through Program.
/// </summary>
public static class AuditDatabase
{
    /// <summary>
    /// Applies pending migrations before the worker starts or the API accepts requests.
    /// </summary>
    /// <param name="app">The built application providing scoped services, logging, and shutdown cancellation.</param>
    /// <returns>A task representing database initialization.</returns>
    public static async Task InitializeAsync(WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        AuditDbContext database = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        try
        {
            await database.Database.MigrateAsync(app.Lifetime.ApplicationStopping);
        }
        catch (Exception exception)
        {
            app.Logger.LogCritical(exception, "Audit database initialization failed. The API cannot start.");
            throw;
        }
    }

    /// <summary>
    /// Reads the SQLite connection string, resolves its file path, and creates its parent directory.
    /// </summary>
    /// <param name="configuration">Contains ConnectionStrings:AuditDatabase.</param>
    /// <param name="environment">Provides the API content root for relative file paths.</param>
    /// <returns>The connection string</returns>
    /// <exception cref="InvalidOperationException">The connection string is missing or does not specify a SQLite file path.</exception>
    public static string GetConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        string? configured = configuration.GetConnectionString("AuditDatabase");
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException("ConnectionStrings:AuditDatabase must be configured.");
        }

        var connection = new SqliteConnectionStringBuilder(configured);
        if (string.IsNullOrWhiteSpace(connection.DataSource))
        {
            throw new InvalidOperationException(
                "The audit database connection string must specify a SQLite file path.");
        }

        connection.DataSource = Path.GetFullPath(connection.DataSource, environment.ContentRootPath);
        Directory.CreateDirectory(Path.GetDirectoryName(connection.DataSource)!);
        return connection.ToString();
    }
}
