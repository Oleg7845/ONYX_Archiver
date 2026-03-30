namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when the system fails to update an existing peer's information in the persistence layer.
/// This is critical during key rotation or when finalizing a handshake response.
/// </summary>
/// <remarks>
/// Common causes include database write-locks, unique constraint violations (e.g., if a new 
/// Public Key is already assigned to another PeerID), or attempting to update a record 
/// that was deleted in another thread.
/// </remarks>
public class PeerUpdatingException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PeerUpdatingException"/> class.
    /// </summary>
    /// <param name="peerId">The unique cryptographic identifier (GUID) of the peer whose update failed.</param>
    public PeerUpdatingException(Guid peerId)
        : base(userMessage: $"Failed to update the contact information. Peer ID: '{peerId}'")
    {
    }

    /// <summary>
    /// Initializes a new instance with specific technical context for debugging database or I/O failures.
    /// </summary>
    /// <param name="technicalMessage">Details like "Concurrency conflict: row version mismatch" or "SQLite database is locked."</param>
    /// <param name="peerId">The unique identifier of the peer.</param>
    public PeerUpdatingException(string technicalMessage, Guid peerId)
        : base(
            userMessage: "An error occurred while saving the updated contact details.",
            technicalMessage: $"PeerId: {peerId} | Error: {technicalMessage}")
    {
    }
}