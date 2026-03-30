using OnyxArchiver.Domain.Entities;

namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Defines the contract for persisting and retrieving peer (contact) information.
/// Acts as an abstraction layer between the domain services and the underlying database.
/// </summary>
public interface IPeerRepository
{
    /// <summary>
    /// Retrieves all peers associated with a specific user.
    /// </summary>
    /// <param name="username">The identifier of the user who owns the contact list.</param>
    /// <returns>A list of <see cref="PeerEntity"/>, or null if none are found.</returns>
    Task<List<PeerEntity>?> GetAllPeersAsync(string username);

    /// <summary>
    /// Retrieves a subset of peers for pagination (Infinite Scroll).
    /// </summary>
    /// <param name="username">The identifier of the user.</param>
    /// <param name="offset">The number of records to skip.</param>
    /// <param name="limit">The maximum number of records to return.</param>
    Task<List<PeerEntity>?> GetPeersBatchAsync(string username, int offset, int limit);

    /// <summary>
    /// Finds a specific peer by their unique id.
    /// </summary>
    Task<PeerEntity?> GetByPeerByIdAsync(int id);

    /// <summary>
    /// Finds a specific peer by their unique cryptographic GUID.
    /// </summary>
    Task<PeerEntity?> GetByPeerByPeerIdAsync(Guid peerId);

    /// <summary>
    /// Finds a specific peer by their name.
    /// </summary>
    Task<PeerEntity?> GetByPeerByNameAsync(string name);

    /// <summary>
    /// Saves a new peer record to the database.
    /// </summary>
    /// <returns>True if the operation was successful.</returns>
    Task<bool> AddAsync(PeerEntity peer);

    /// <summary>
    /// Removes a peer using the primary database ID.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Removes a peer using their cryptographic identity (GUID).
    /// </summary>
    Task<bool> DeleteAsync(Guid peerId);

    /// <summary>
    /// Updates an existing peer's information (e.g., name or finalized status).
    /// </summary>
    Task<bool> UpdateAsync(PeerEntity peer);

    /// <summary>
    /// Checks if a peer with the specified identity already exists in the system.
    /// </summary>
    Task<bool> ExistsAsync(Guid peerId);
}
