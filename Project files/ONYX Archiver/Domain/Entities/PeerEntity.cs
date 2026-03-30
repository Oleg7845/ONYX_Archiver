namespace OnyxArchiver.Domain.Entities;

/// <summary>
/// Represents a security-focused contact (peer) in the local database.
/// Stores public and encrypted private keys required for secure key exchange and file encryption.
/// </summary>
public class PeerEntity
{
    /// <summary> 
    /// Gets or sets the internal database primary key. 
    /// </summary>
    public int Id { get; set; }

    /// <summary> 
    /// Gets or sets the local username associated with this peer contact. 
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary> 
    /// Gets or sets the unique cryptographic identity identifier (GUID) for the peer. 
    /// </summary>
    public Guid PeerId { get; set; }

    /// <summary> 
    /// Gets or sets the human-readable display name for the contact. 
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary> 
    /// Gets or sets the salt used for deriving session keys specific to this peer interaction. 
    /// </summary>
    public byte[] Salt { get; set; } = [];

    /// <summary> 
    /// Gets or sets the local ephemeral public key generated for this peer context. 
    /// </summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary> 
    /// Gets or sets the encrypted local private X25519 key used for Diffie-Hellman key exchange. 
    /// This key should never be stored in plaintext.
    /// </summary>
    public byte[] LocalEncryptionPrivateKey { get; set; } = [];

    /// <summary> 
    /// Gets or sets the encrypted local private Ed25519 key used for signing handshake data. 
    /// Ensures that only the legitimate owner can initiate a session.
    /// </summary>
    public byte[] LocalSignaturePrivateKey { get; set; } = [];

    /// <summary> 
    /// Gets or sets the recipient's public encryption key obtained after a successful handshake. 
    /// Used for encrypting data specifically for this peer.
    /// </summary>
    public byte[]? RecipientEncryptionPublicKey { get; set; } = [];

    /// <summary> 
    /// Gets or sets the recipient's public signature key used for verifying their identity. 
    /// Prevents Man-in-the-Middle (MITM) attacks.
    /// </summary>
    public byte[]? RecipientSignaturePublicKey { get; set; } = [];

    /// <summary> 
    /// Gets or sets the timestamp when the peer record was initially created in the system. 
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary> 
    /// Gets or sets the timestamp when the handshake process was fully completed. 
    /// If null, the peer connection is considered pending or unverified. 
    /// </summary>
    public DateTime? FinalizedAt { get; set; }
}