using CryptoCore.Abstractions;

namespace OnyxArchiver.Core.Models.Cryptography;

/// <summary>
/// Holds the state and cryptographic components required for an active encryption session.
/// Encapsulates the encryption engine alongside the metadata needed to derive or identify the keys.
/// </summary>
/// <remarks>
/// This context acts as a single point of access for the archiver logic to protect data blocks,
/// ensuring that the salt and public keys are kept together with the active cipher.
/// </remarks>
public class EncryptionContext : IDisposable
{
    /// <summary>
    /// Gets the unique salt used for key derivation (KDF) in this context.
    /// Essential for preventing rainbow table attacks and ensuring unique keys.
    /// </summary>
    public byte[] Salt { get; init; }

    /// <summary>
    /// Gets the public key associated with this encryption session.
    /// Used in asymmetric or hybrid schemes (like X25519) to identify the recipient or the session key.
    /// </summary>
    public byte[] PublicKey { get; init; }

    /// <summary>
    /// Gets the active symmetric encryption engine (e.g., XChaCha20Poly1305).
    /// Responsible for the actual transformation of plaintext into ciphertext.
    /// </summary>
    public IEncrypter Encrypter { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionContext"/> class.
    /// </summary>
    /// <param name="salt">The cryptographic salt for key derivation.</param>
    /// <param name="publicKey">The public key for the encryption session.</param>
    /// <param name="encrypter">The prepared encryption provider.</param>
    public EncryptionContext(byte[] salt, byte[] publicKey, IEncrypter encrypter)
    {
        Salt = salt;
        PublicKey = publicKey;
        Encrypter = encrypter;
    }

    /// <summary>
    /// Securely disposes the underlying encrypter and wipes any sensitive session data.
    /// </summary>
    public void Dispose()
    {
        Encrypter.Dispose();
    }
}
