using CryptoCore.Abstractions;
using OnyxArchiver.Core.Models.Cryptography;

namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Provides access to the current user's active cryptographic tools and identity.
/// Acts as a volatile memory bridge between stored encrypted keys and active encryption providers.
/// </summary>
/// <remarks>
/// This service is the only authorized way to access cryptographic operations. 
/// It ensures that private keys remain protected in memory and are never exposed 
/// to the UI or high-level business logic in plaintext.
/// </remarks>
public interface IUserVaultService
{
    /// <summary>
    /// Retrieves the full cryptographic context of the currently authenticated user.
    /// This context includes the master encrypter, the user's public identity key, and unique session salt.
    /// </summary>
    /// <exception cref="EncryptionContextException">Thrown if no user is currently logged in or the session has expired.</exception>
    /// <returns>An <see cref="EncryptionContext"/> containing the session's active security parameters.</returns>
    Task<EncryptionContext> GetEncryptionContext();

    /// <summary>
    /// Generates a symmetric encrypter (specifically XChaCha20-Poly1305) derived from 
    /// a specific salt and public key.
    /// </summary>
    /// <remarks>
    /// This is used for "Peer Encryption." It combines the user's private key with the 
    /// recipient's public key to derive a unique, one-time session key.
    /// </remarks>
    /// <param name="salt">The salt associated with the specific peer or archive operation.</param>
    /// <param name="publicKey">The public key of the recipient (Peer) used for key derivation.</param>
    /// <returns>A thread-safe <see cref="IEncrypter"/> instance.</returns>
    Task<IEncrypter> GetEncrypter(byte[] salt, byte[] publicKey);

    /// <summary>
    /// Generates a symmetric decrypter (specifically XChaCha20-Poly1305) derived from 
    /// a specific salt and public key.
    /// </summary>
    /// <remarks>
    /// This is used during "Archive Extraction." It reverses the encryption applied 
    /// using the corresponding parameters.
    /// </remarks>
    /// <param name="salt">The salt originally used during the encryption phase.</param>
    /// <param name="publicKey">The public key of the sender required to reconstruct the shared secret.</param>
    /// <returns>A thread-safe <see cref="IDecrypter"/> instance.</returns>
    Task<IDecrypter> GetDecrypter(byte[] salt, byte[] publicKey);
}