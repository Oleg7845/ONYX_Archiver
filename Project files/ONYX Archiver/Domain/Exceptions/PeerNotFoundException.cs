namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when a requested peer cannot be located in the persistence layer (database).
/// This is a critical failure during archive creation or decryption because the 
/// system cannot access the necessary public keys for the cryptographic operation.
/// </summary>
/// <remarks>
/// This often occurs if a user attempts to open an archive from a sender they have 
/// previously deleted, or if a database sync error has occurred.
/// </remarks>
public class PeerNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PeerNotFoundException"/> class.
    /// </summary>
    /// <param name="peerId">The unique cryptographic identifier (GUID) of the missing peer.</param>
    public PeerNotFoundException(Guid? peerId = null)
        : base(userMessage: peerId.HasValue
              ? $"The contact (ID: {peerId}) could not be found in your database."
              : "The requested contact does not exist.")
    {
    }

    /// <summary>
    /// Initializes a new instance with a technical context, such as which repository method failed.
    /// </summary>
    /// <param name="technicalMessage">Details like "FirstOrDefault returned null in PeerRepository.GetByIdAsync".</param>
    /// <param name="peerId">The unique identifier of the missing peer.</param>
    public PeerNotFoundException(string technicalMessage, Guid? peerId = null)
        : base(
            userMessage: "The security contact information is missing.",
            technicalMessage: $"PeerId: {peerId} | Source: {technicalMessage}")
    {
    }
}