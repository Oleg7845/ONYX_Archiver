namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Provides an abstraction for OS-level protected storage (e.g., DPAPI, Keychain).
/// Used for persisting the "Root" or "Master" keying material that must not reside 
/// in the standard application database.
/// </summary>
/// <remarks>
/// This service acts as the anchor for the entire trust chain. Implementations must 
/// ensure that the data is encrypted using the user's OS-level credentials, making 
/// the keys inaccessible if the hard drive is moved to a different machine.
/// </remarks>
public interface ISecureStorageService
{
    /// <summary>
    /// Encrypts and saves a sensitive cryptographic key to a persistent, OS-hardened location.
    /// </summary>
    /// <param name="fileName">A unique identifier for the key (e.g., the user's local ID or username).</param>
    /// <param name="key">The raw byte array of the sensitive keying material to be protected.</param>
    /// <exception cref="SecureStorageException">Thrown if the OS-level security service is unavailable.</exception>
    void SaveKey(string fileName, byte[] key);

    /// <summary>
    /// Retrieves and decrypts a previously saved cryptographic key using the current user's OS context.
    /// </summary>
    /// <param name="fileName">The unique identifier associated with the stored key.</param>
    /// <returns>The decrypted byte array of the keying material.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if no key exists for the provided fileName.</exception>
    /// <exception cref="SecureStorageException">Thrown if the OS denies access to the secure vault.</exception>
    byte[] LoadKey(string fileName);

    /// <summary>
    /// Permanently and securely removes the encrypted key from the OS-level storage.
    /// </summary>
    /// <param name="fileName">The unique identifier of the key to be purged.</param>
    void DeleteKey(string fileName);
}