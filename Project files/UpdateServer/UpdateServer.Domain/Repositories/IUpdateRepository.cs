using UpdateServer.Domain.Entities;

namespace UpdateServer.Domain.Repositories;

/// <summary>
/// Defines the data access contract for managing applications and their associated releases.
/// This abstraction allows the application layer to remain independent of the underlying 
/// database technology (e.g., PostgreSQL, SQL Server).
/// </summary>
public interface IUpdateRepository
{
    /// <summary>
    /// Retrieves an application entity by its unique internal name.
    /// Used during the initial stage of the update check to validate the existence of the product.
    /// </summary>
    /// <param name="name">The case-sensitive name of the application (e.g., "OnyxArchiver").</param>
    /// <returns>The <see cref="Application"/> if found; otherwise, null.</returns>
    Task<Application?> GetApplicationByNameAsync(string name);

    /// <summary>
    /// Finds the most recent release for a specific application within a given deployment channel.
    /// Implementation should typically sort by <see cref="Release.Version"/> or <see cref="Release.CreatedAt"/> descending.
    /// </summary>
    /// <param name="applicationId">The unique ID of the application.</param>
    /// <param name="channel">The name of the channel to search in (e.g., "stable").</param>
    /// <returns>
    /// The latest <see cref="Release"/> including its associated <see cref="ReleaseFile"/> collection, 
    /// or null if no releases exist for this channel.
    /// </returns>
    Task<Release?> GetLatestReleaseAsync(Guid applicationId, string channel);

    /// <summary>
    /// Registers a new release in the system. 
    /// Note: This method only tracks the entity in the change tracker; 
    /// use <see cref="SaveChangesAsync"/> to commit to the database.
    /// </summary>
    /// <param name="release">The populated release entity with associated files.</param>
    Task AddReleaseAsync(Release release);

    /// <summary>
    /// Commits all pending changes (inserts, updates, deletes) to the persistent storage.
    /// Implements the Unit of Work pattern.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    Task SaveChangesAsync();
}