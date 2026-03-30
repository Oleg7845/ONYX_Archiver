using Microsoft.EntityFrameworkCore;
using OnyxArchiver.Domain.Entities;
using OnyxArchiver.Domain.Interfaces;
using System.Diagnostics;

namespace OnyxArchiver.Infrastructure.Repositories;

/// <summary>
/// Provides an implementation of the <see cref="IPeerRepository"/> interface for managing 
/// peer metadata using Entity Framework Core.
/// </summary>
/// <remarks>
/// Each method creates a fresh <see cref="ApplicationDbContext"/> via the factory to ensure 
/// thread safety and prevent context bloating in long-running desktop sessions.
/// </remarks>
public class PeerRepository : IPeerRepository
{
    private readonly IDbContextFactory _contextFactory;

    public PeerRepository(IDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Persists a new peer entity to the database.
    /// </summary>
    /// <param name="peer">The peer data to save.</param>
    /// <returns><c>true</c> if successfully created; otherwise, <c>false</c>.</returns>
    public async Task<bool> AddAsync(PeerEntity peer)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                await context.Peers.AddAsync(peer);
                await context.SaveChangesAsync();
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Creation Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Finds a specific peer by their unique id <see cref="int"/>.
    /// Uses <c>AsNoTracking()</c> for high-performance, read-only access.
    /// </summary>
    /// <param name="Id">The unique ID of the peer.</param>
    /// <returns>The <see cref="PeerEntity"/> or null if not found.</returns>
    public async Task<PeerEntity?> GetByPeerByIdAsync(int id)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context.Peers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Record extract Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Finds a specific peer by their unique cryptographic <see cref="Guid"/>.
    /// Uses <c>AsNoTracking()</c> for high-performance, read-only access.
    /// </summary>
    /// <param name="peerId">The unique PeerID of the peer.</param>
    /// <returns>The <see cref="PeerEntity"/> or null if not found.</returns>
    public async Task<PeerEntity?> GetByPeerByPeerIdAsync(Guid peerId)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context.Peers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PeerId == peerId);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Record extract Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Finds a specific peer by name <see cref="string"/>.
    /// Uses <c>AsNoTracking()</c> for high-performance, read-only access.
    /// </summary>
    /// <param name="peerId">The name of the peer.</param>
    /// <returns>The <see cref="PeerEntity"/> or null if not found.</returns>
    public async Task<PeerEntity?> GetByPeerByNameAsync(string name)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context.Peers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Name == name);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Record extract Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Retrieves all peers associated with a specific local user profile.
    /// </summary>
    /// <param name="username">The local user's name.</param>
    /// <returns>A list of peers or null if an error occurs.</returns>
    public async Task<List<PeerEntity>?> GetAllPeersAsync(string username)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context.Peers
                    .AsNoTracking()
                    .Where(p => p.Username == username)
                    .OrderBy(p => p.Id)
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Extract all peers error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Performs paginated extraction of peers to optimize memory for large address books.
    /// </summary>
    /// <param name="username">The local user's name.</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <param name="limit">Number of records to take.</param>
    public async Task<List<PeerEntity>?> GetPeersBatchAsync(string username, int offset, int limit)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context.Peers
                    .AsNoTracking()
                    .Where(p => p.Username == username)
                    .OrderBy(p => p.Id)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Batch extract Error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Updates an existing peer's information (e.g., changing a display name or updating a public key).
    /// </summary>
    public async Task<bool> UpdateAsync(PeerEntity peer)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                context.Peers.Update(peer);
                return await context.SaveChangesAsync() > 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Update Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes a peer from the database by its primary key ID.
    /// </summary>
    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var entity = await context.Peers.FirstOrDefaultAsync(p => p.Id == id);

                if (entity == null) return false;

                context.Peers.Remove(entity);
                return await context.SaveChangesAsync() > 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Delete Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes a peer from the database by its unique identity <see cref="Guid"/>.
    /// </summary>
    public async Task<bool> DeleteAsync(Guid peerId)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                var entity = await context.Peers.FirstOrDefaultAsync(p => p.PeerId == peerId);

                if (entity == null) return false;

                context.Peers.Remove(entity);
                return await context.SaveChangesAsync() > 0;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Delete Error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Checks if a peer with the given <see cref="Guid"/> already exists in the local database.
    /// </summary>
    public async Task<bool> ExistsAsync(Guid peerId)
    {
        try
        {
            using (var context = _contextFactory.CreateDbContext())
            {
                return await context.Peers.AnyAsync(p => p.PeerId == peerId);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Exists check Error: {ex.Message}");
            return false;
        }
    }
}
