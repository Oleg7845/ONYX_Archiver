namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when the system fails to establish a new peer connection or 
/// complete a cryptographic key exchange (handshake) process.
/// </summary>
/// <remarks>
/// This exception typically occurs during the processing of .ohs (Onyx Handshake) files,
/// involving issues like invalid public key formats, failed signature verification, 
/// or database write conflicts when saving a new contact.
/// </remarks>
public class PeerCreatingException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PeerCreatingException"/> class 
    /// with a default user-friendly error message.
    /// </summary>
    public PeerCreatingException()
        : base(userMessage: "An error occurred while creating the peer connection.") { }

    /// <summary>
    /// Initializes a new instance with a specific technical reason for the failure.
    /// </summary>
    /// <param name="technicalMessage">Details like "Invalid X25519 public key length" or "Peer ID already exists in DB."</param>
    public PeerCreatingException(string technicalMessage)
        : base(userMessage: "Failed to establish a connection with the peer.", technicalMessage: technicalMessage) { }
}