namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when the system encounters an error while attempting to remove a peer 
/// and their associated cryptographic material from the local database.
/// </summary>
/// <remarks>
/// Deletion failures can occur due to database deadlocks, referential integrity 
/// constraints (e.g., if there are active archives linked to this peer), or 
/// unexpected filesystem permissions if keys are stored in external files.
/// </remarks>
public class PeerDeletingException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PeerDeletingException"/> class.
    /// </summary>
    /// <param name="peerId">The unique cryptographic identifier (GUID) of the peer that could not be removed.</param>
    public PeerDeletingException(Guid? peerId = null)
        : base(userMessage: peerId.HasValue
              ? $"Failed to delete the peer contact. Identity: {peerId}"
              : "An error occurred while attempting to delete the peer contact.")
    {
    }

    /// <summary>
    /// Initializes a new instance with a specific technical reason for the deletion failure.
    /// </summary>
    /// <param name="technicalMessage">Details like "SQL Foreign Key constraint violation" or "DB file is read-only."</param>
    /// <param name="peerId">The unique identifier of the peer.</param>
    public PeerDeletingException(string technicalMessage, Guid? peerId = null)
        : base(
            userMessage: "The peer could not be removed from the database.",
            technicalMessage: $"PeerId: {peerId} | Error: {technicalMessage}")
    {
    }
}