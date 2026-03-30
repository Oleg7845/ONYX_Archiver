using CryptoCore.Abstractions;

namespace CryptoCore.Cryptography.Encryption.XChaCha20;

/// <summary>
/// A high-level provider that bridges the <see cref="XChaCha20Poly1305"/> implementation 
/// with the application's core encryption and decryption abstractions.
/// </summary>
/// <remarks>
/// This class simplifies cryptographic operations by storing default parameters like 
/// associated data and padding boundaries, allowing for a cleaner API usage.
/// </remarks>
public sealed class XChaCha20Poly1305Provider : IEncrypter, IDecrypter, IDisposable
{
    private readonly XChaCha20Poly1305 _cipher;
    private readonly byte[]? _associatedData;
    private readonly int _paddingBoundary;

    /// <summary>
    /// Gets the overhead size (in bytes) added to each encrypted packet by this provider.
    /// </summary>
    public int PacketHeaderSize { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="XChaCha20Poly1305Provider"/> with persistent encryption settings.
    /// </summary>
    /// <param name="key">The 32-byte symmetric key for XChaCha20.</param>
    /// <param name="associatedData">Default non-secret data to be authenticated with every operation.</param>
    /// <param name="paddingBoundary">The default block size for data alignment (padding) to obscure length.</param>
    public XChaCha20Poly1305Provider(
        byte[] key,
        byte[]? associatedData = null,
        int paddingBoundary = 0)
    {
        _cipher = new XChaCha20Poly1305(key);
        _associatedData = associatedData;
        _paddingBoundary = paddingBoundary;

        PacketHeaderSize = XChaCha20Poly1305.PacketHeaderSize;
    }

    /// <summary>
    /// Encrypts the provided data using the default settings initialized in the constructor.
    /// </summary>
    /// <param name="data">The plaintext data to encrypt.</param>
    /// <returns>A fully formatted encrypted packet.</returns>
    public byte[] Encrypt(ReadOnlySpan<byte> data)
    {
        // Pass the call to the main class with all saved settings
        return _cipher.Encrypt(data, _associatedData, _paddingBoundary);
    }

    /// <summary>
    /// Encrypts data while overriding the default associated data and padding settings for this specific call.
    /// </summary>
    /// <param name="data">The plaintext data to encrypt.</param>
    /// <param name="associatedData">Custom non-secret data for authentication.</param>
    /// <param name="paddingBoundary">Custom block size for data padding.</param>
    /// <returns>A fully formatted encrypted packet.</returns>
    public byte[] Encrypt(ReadOnlySpan<byte> data, byte[]? associatedData = null, int paddingBoundary = 0)
    {
        // Pass the call to the main class with all saved settings
        return _cipher.Encrypt(data, associatedData, paddingBoundary);
    }

    /// <summary>
    /// Decrypts the provided packet using the default associated data setting.
    /// </summary>
    /// <param name="data">The encrypted packet bytes.</param>
    /// <returns>The original decrypted plaintext.</returns>
    public byte[] Decrypt(ReadOnlySpan<byte> data)
    {
        return _cipher.Decrypt(data, _associatedData);
    }

    /// <summary>
    /// Decrypts a packet while providing custom associated data for this specific verification.
    /// </summary>
    /// <param name="data">The encrypted packet bytes.</param>
    /// <param name="associatedData">The custom non-secret data that was used during encryption.</param>
    /// <returns>The original decrypted plaintext.</returns>
    public byte[] Decrypt(ReadOnlySpan<byte> data, byte[]? associatedData = null)
    {
        return _cipher.Decrypt(data, associatedData);
    }

    /// <summary>
    /// Disposes the underlying cipher and ensures cryptographic keys are cleared.
    /// </summary>
    public void Dispose()
    {
        _cipher?.Dispose();
    }
}
