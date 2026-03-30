using Microsoft.EntityFrameworkCore;
using UpdateServer.Domain.Entities;
using UpdateServer.Domain.Repositories;
using UpdateServer.Infrastructure.Data;

namespace UpdateServer.Infrastructure.Repositories;

/// <summary>
/// Implements the <see cref="IUpdateRepository"/> using Entity Framework Core.
/// Handles optimized database queries with deep inclusion of related update metadata.
/// </summary>
public class UpdateRepository : IUpdateRepository
{
    private readonly AppDbContext _db;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateRepository"/>.
    /// </summary>
    /// <param name="db">The database context instance.</param>
    public UpdateRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Retrieves an application by its name, eagerly loading the entire hierarchy of 
    /// channels, releases, and files for comprehensive metadata access.
    /// </summary>
    public async Task<Application?> GetApplicationByNameAsync(string name)
    {
        // Using .Include and .ThenInclude to avoid multiple database roundtrips.
        return await _db.Applications
            .Include(a => a.Channels)
                .ThenInclude(c => c.Releases)
                    .ThenInclude(r => r.Files)
            .FirstOrDefaultAsync(a => a.Name == name);
    }

    /// <summary>
    /// Fetches the most recent release for a specific application and channel, 
    /// ordered by the creation date to ensure the "Latest" status is accurate.
    /// </summary>
    public async Task<Release?> GetLatestReleaseAsync(Guid applicationId, string channel)
    {
        return await _db.Releases
            .Include(r => r.Files) // Ensure file metadata (Hash, Signature) is available.
            .Where(r => r.Channel.ApplicationId == applicationId && r.Channel.Name == channel)
            .OrderByDescending(r => r.CreatedAt) // Sort by most recent first.
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Persists a new release record. 
    /// Note: This adds the entity to the Change Tracker.
    /// </summary>
    public async Task AddReleaseAsync(Release release)
    {
        await _db.Releases.AddAsync(release);
    }

    /// <summary>
    /// Commits all staged changes to the database in a single transaction.
    /// </summary>
    public async Task SaveChangesAsync()
    {
        await _db.SaveChangesAsync();
    }
}