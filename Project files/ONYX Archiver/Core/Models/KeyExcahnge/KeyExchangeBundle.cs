namespace OnyxArchiver.Core.Models.KeyExcahnge;

/// <summary>
/// Encapsulates the complete set of data produced during a key exchange operation.
/// This bundle contains both the public <see cref="KeyExchangeContext"/> (intended for the peer) 
/// and the encrypted private keys (intended for local persistent storage).
/// </summary>
/// <remarks>
/// To ensure security, private keys stored in this bundle must be encrypted 
/// using a master key or derivation provider before assignment.
/// </remarks>
public class KeyExchangeBundle
{
    /// <summary>
    /// Gets or sets the public context which includes versioning, unique identifiers, 
    /// public keys, and digital signatures to be shared with the other party.
    /// </summary>
    public KeyExchangeContext Context { get; set; }

    /// <summary>
    /// Gets or sets the local X25519 private key, stored in an encrypted format.
    /// This key is required to compute the shared secret once the remote public key is obtained.
    /// </summary>
    public byte[] PrivateEncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the local Ed25519 private key, stored in an encrypted format.
    /// Used for authenticating future messages or verifying the ownership of the session.
    /// </summary>
    public byte[] PrivateSignatureKey { get; set; }

    /// <summary>
    /// Gets or sets the encrypted public encryption key received from the remote party.
    /// Storing this allows the application to reconstruct the exchange state later.
    /// </summary>
    public byte[]? PublicEncryptionKey { get; set; }

    /// <summary>
    /// Gets or sets the encrypted public signature key received from the remote party.
    /// This is used to verify the authenticity of the peer in subsequent communications.
    /// </summary>
    public byte[]? PublicSignatureKey { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyExchangeBundle"/> class.
    /// </summary>
    /// <param name="context">The public signed context for transmission.</param>
    /// <param name="privateEncryptionKey">The encrypted local private encryption key.</param>
    /// <param name="privateSignatureKey">The encrypted local private signature key.</param>
    /// <param name="publicEncryptionKey">Optional encrypted remote public encryption key.</param>
    /// <param name="publicSignatureKey">Optional encrypted remote public signature key.</param>
    public KeyExchangeBundle(
        KeyExchangeContext context,
        byte[] privateEncryptionKey,
        byte[] privateSignatureKey,
        byte[]? publicEncryptionKey = null,
        byte[]? publicSignatureKey = null)
    {
        Context = context;
        PrivateEncryptionKey = privateEncryptionKey;
        PrivateSignatureKey = privateSignatureKey;
        PublicEncryptionKey = publicEncryptionKey;
        PublicSignatureKey = publicSignatureKey;
    }
}
