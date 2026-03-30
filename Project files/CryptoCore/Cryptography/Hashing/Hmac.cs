using NSec.Cryptography;

namespace CryptoCore.Cryptography.Hashing;

/// <summary>
/// Provides Message Authentication Code (MAC) functionality using the HMAC algorithm.
/// Ensures data integrity and authenticity by creating a cryptographic "tag" for a given data block.
/// </summary>
public sealed class Hmac : IDisposable
{
    private readonly Key _key;
    private readonly MacAlgorithm _algorithm;

    /// <summary>
    /// Gets the size of the authentication tag produced by the current algorithm in bytes.
    /// </summary>
    public int MacSize => _algorithm.MacSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="Hmac"/> class.
    /// Automatically selects the algorithm (HMAC-SHA256 or HMAC-SHA512) based on the provided key length.
    /// </summary>
    /// <param name="keyBytes">The symmetric key. Must be 32 bytes for SHA-256 or 64 bytes for SHA-512.</param>
    /// <exception cref="ArgumentException">Thrown when the key length does not match supported algorithms.</exception>
    public Hmac(byte[] keyBytes)
    {
        _algorithm = keyBytes.Length switch
        {
            32 => MacAlgorithm.HmacSha256,
            64 => MacAlgorithm.HmacSha512,
            _ => throw new ArgumentException("The key must be either 32 bytes (SHA256) or 64 bytes (SHA512).")
        };

        // Import the raw bytes into a secure Key object managed by NSec
        _key = Key.Import(_algorithm, keyBytes, KeyBlobFormat.RawSymmetricKey);
    }

    /// <summary>
    /// Computes the authentication tag for the specified data.
    /// </summary>
    /// <param name="data">The data to be authenticated.</param>
    /// <returns>A byte array containing the computed MAC tag.</returns>
    public byte[] Sign(ReadOnlySpan<byte> data)
    {
        return _algorithm.Mac(_key, data);
    }

    /// <summary>
    /// Verifies the authenticity of the data by comparing it against a provided MAC tag.
    /// </summary>
    /// <remarks>
    /// This method uses constant-time comparison to protect against timing side-channel attacks.
    /// </remarks>
    /// <param name="data">The original data that was received.</param>
    /// <param name="tag">The MAC tag to verify against.</param>
    /// <returns>True if the tag is valid and the data is authentic; otherwise, false.</returns>
    public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> tag)
    {
        return _algorithm.Verify(_key, data, tag);
    }

    /// <summary>
    /// Securely releases the cryptographic key from memory.
    /// </summary>
    public void Dispose()
    {
        _key?.Dispose();
    }
}
