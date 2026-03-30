namespace OnyxArchiver.Domain.Entities;

/// <summary>
/// Represents the local user's account and cryptographic identity.
/// Stores the protected master keys and authentication credentials.
/// </summary>
public class UserEntity
{
    /// <summary> 
    /// Gets or sets the primary key for the database record. 
    /// </summary>
    public int Id { get; set; }

    /// <summary> 
    /// Gets or sets the unique username used for local identification and key derivation. 
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary> 
    /// Gets or sets the Argon2id hash of the master password. 
    /// Used solely for authentication during the login process.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary> 
    /// Gets or sets the JSON-encoded configuration for the Argon2id algorithm (iterations, memory, parallelism).
    /// This ensures the password can be verified even if global security settings change over time.
    /// </summary>
    public string KeyDescriptor { get; set; } = string.Empty;

    /// <summary> 
    /// Gets or sets the cryptographically strong salt used in the second stage of key derivation (HKDF).
    /// This salt is combined with the user's password to generate the Master Encryption Key.
    /// </summary>
    public byte[] Salt { get; set; } = [];

    /// <summary> 
    /// Gets or sets the X25519 private key, encrypted by the derived master key.
    /// This key is required for decrypting incoming archives and establishing secure handshakes with peers.
    /// </summary>
    public byte[] EncryptionPrivateKey { get; set; } = [];

    /// <summary> 
    /// Gets or sets the Ed25519 private key, encrypted by the derived master key.
    /// This key is used for signing archives and proving the user's identity to others.
    /// </summary>
    public byte[] SignaturePrivateKey { get; set; } = [];

    /// <summary> 
    /// Gets or sets the timestamp of account registration. 
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}