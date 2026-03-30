using OnyxArchiver.Infrastructure.Data;

namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Defines a factory for creating instances of <see cref="ApplicationDbContext"/>.
/// Orchestrates the database lifecycle to ensure thread safety and data consistency.
/// </summary>
/// <remarks>
/// In a multi-threaded desktop environment, this factory is essential for:
/// 1. Preventing concurrency conflicts when background tasks (e.g., file hashing) 
///    and UI tasks (e.g., list refreshing) access the database at the same time.
/// 2. Ensuring that each operation starts with a clean "Change Tracker" state.
/// 3. Safely handling the creation of the SQLite file in the local app data folder.
/// </remarks>
public interface IDbContextFactory
{
    /// <summary>
    /// Creates and configures a fresh instance of the application database context.
    /// </summary>
    /// <returns>A new <see cref="ApplicationDbContext"/> configured for the current session.</returns>
    /// <remarks>
    /// The caller is responsible for disposing of the context, typically via a 'using' statement.
    /// </remarks>
    ApplicationDbContext CreateDbContext();
}