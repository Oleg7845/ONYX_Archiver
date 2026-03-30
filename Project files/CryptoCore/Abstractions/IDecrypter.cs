namespace CryptoCore.Abstractions;

/// <summary>
/// Defines the contract for symmetric data decryption services.
/// Implementing classes provide functionality to restore original plaintext from secure encrypted packets 
/// while verifying data integrity and authenticity.
/// </summary>
public interface IDecrypter : IDisposable
{
    /// <summary>
    /// Decrypts the provided secure packet using default provider settings.
    /// </summary>
    /// <param name="data">The encrypted packet bytes (Ciphertext + Metadata). 
    /// Uses <see cref="ReadOnlySpan{T}"/> for high-performance, zero-copy memory access.</param>
    /// <returns>A byte array containing the original plaintext.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown if decryption or 
    /// authentication (MAC verification) fails.</exception>
    byte[] Decrypt(ReadOnlySpan<byte> data);

    /// <summary>
    /// Decrypts a secure packet while providing specific associated data for authentication.
    /// </summary>
    /// <remarks>
    /// This method is used with AEAD algorithms to ensure that the encrypted data is cryptographically 
    /// bound to the provided non-secret metadata.
    /// </remarks>
    /// <param name="data">The encrypted packet bytes.</param>
    /// <param name="associatedData">The non-secret metadata that was bound to the ciphertext during encryption. 
    /// Decryption will strictly fail if this data does not match the original.</param>
    /// <returns>A byte array containing the original plaintext.</returns>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Thrown if the integrity check 
    /// fails or the associated data is incorrect.</exception>
    byte[] Decrypt(ReadOnlySpan<byte> data, byte[]? associatedData = null);
}
