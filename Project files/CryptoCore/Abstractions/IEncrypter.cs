namespace CryptoCore.Abstractions;

/// <summary>
/// Defines the contract for symmetric data encryption services.
/// Implementing classes provide confidentiality and optionally integrity/authenticity 
/// by transforming plaintext into secure encrypted packets.
/// </summary>
public interface IEncrypter : IDisposable
{
    /// <summary>
    /// Encrypts input data using default provider settings and returns a secure packet.
    /// </summary>
    /// <param name="data">The raw plaintext bytes to be protected. 
    /// Use <see cref="ReadOnlySpan{T}"/> for memory-efficient access.</param>
    /// <returns>A byte array containing the encrypted ciphertext along with any necessary metadata (e.g., nonces, tags).</returns>
    byte[] Encrypt(ReadOnlySpan<byte> data);

    /// <summary>
    /// Encrypts input data with fine-grained control over authentication and data alignment.
    /// </summary>
    /// <remarks>
    /// This overload supports AEAD (Authenticated Encryption with Associated Data) 
    /// and length-hiding through padding boundaries.
    /// </remarks>
    /// <param name="data">The raw plaintext bytes to be protected.</param>
    /// <param name="associatedData">Optional non-secret metadata that must be cryptographically bound to the ciphertext 
    /// (e.g., headers or IDs). If modified, decryption will fail.</param>
    /// <param name="paddingBoundary">The block size used for data alignment. 
    /// Padding obscures the exact size of the original file, protecting against traffic analysis.</param>
    /// <returns>A byte array containing the versioned encrypted packet.</returns>
    byte[] Encrypt(ReadOnlySpan<byte> data, byte[]? associatedData = null, int paddingBoundary = 0);
}
