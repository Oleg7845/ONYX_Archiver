using Microsoft.EntityFrameworkCore;
using OnyxArchiver.Domain.Interfaces;

namespace OnyxArchiver.Infrastructure.Data;

/// <summary>
/// Provides a factory for creating instances of <see cref="ApplicationDbContext"/>.
/// Handles the initialization and lifecycle management of the SQLite database.
/// </summary>
/// <remarks>
/// Using a factory pattern ensures that short-lived context instances are used for each operation, 
/// which is a best practice for desktop applications to avoid memory leaks and stale data.
/// </remarks>
public class DbContextFactory : IDbContextFactory
{
    private readonly IAppService _appService;

    public DbContextFactory(IAppService appService)
    {
        _appService = appService;
    }

    /// <summary>
    /// Ensures that the database file exists and the schema is correctly created.
    /// Should be called during the application startup sequence.
    /// </summary>
    /// <remarks>
    /// Uses <c>Database.EnsureCreated()</c> to automatically generate tables based on 
    /// the EF Core model if they do not already exist.
    /// </remarks>
    public void Initialize()
    {
        using (var context = CreateDbContext())
        {
            context.Database.EnsureCreated();

            var connection = context.Database.GetDbConnection();
            connection.Open();
            using (var command = connection.CreateCommand())
            {
               command.CommandText = "PRAGMA journal_mode=WAL;";
                command.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// Creates a new, ready-to-use instance of <see cref="ApplicationDbContext"/>.
    /// </summary>
    /// <returns>A new database context instance.</returns>
    public ApplicationDbContext CreateDbContext()
    {
        return new ApplicationDbContext(_appService.DbFilePath);
    }
}
