using System.Buffers.Binary;
using System.Security.Cryptography;
using NSec.Cryptography;

namespace CryptoCore.Cryptography.Encryption.XChaCha20;

/// <summary>
/// Implements the XChaCha20-Poly1305 AEAD (Authenticated Encryption with Associated Data) algorithm.
/// Provides high-performance encryption with built-in integrity verification and resistance to nonce-misuse.
/// </summary>
/// <remarks>
/// This implementation adds custom packet framing, including versioning and data padding 
/// to hide the original plaintext length.
/// </remarks>
public sealed class XChaCha20Poly1305 : IDisposable
{
    private static readonly AeadAlgorithm Algorithm = AeadAlgorithm.XChaCha20Poly1305;

    /// <summary> The current protocol version of the encrypted packet format. </summary>
    public const byte CurrentVersion = 0x01;

    /// <summary> Required key size in bytes (256-bit). </summary>
    public const int KeySize = 32;

    /// <summary> Extended nonce size in bytes (192-bit) for XChaCha20. </summary>
    public const int NonceSize = 24;

    /// <summary> Poly1305 authentication tag size in bytes (128-bit). </summary>
    public const int TagSize = 16;

    /// <summary> Static size of the packet header including version, padding length, and nonce. </summary>
    public const int PacketHeaderSize = 43;

    private readonly Key _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="XChaCha20Poly1305"/> class with a symmetric key.
    /// </summary>
    /// <param name="keyBytes">The 32-byte secret key.</param>
    /// <exception cref="ArgumentException">Thrown if the key size is not 32 bytes.</exception>
    public XChaCha20Poly1305(byte[] keyBytes)
    {
        if (keyBytes?.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes.");

        _key = Key.Import(Algorithm, keyBytes, KeyBlobFormat.RawSymmetricKey);
    }

    /// <summary>
    /// Encrypts data and assembles a custom packet with metadata.
    /// </summary>
    /// <param name="data">The raw bytes to encrypt.</param>
    /// <param name="associatedData">Optional non-secret data that must be authenticated but not encrypted.</param>
    /// <param name="paddingBoundary">The block size to which the data will be padded (0 for no padding).</param>
    /// <returns>A formatted packet: [Version][PaddingLen][Nonce][Tag][Ciphertext].</returns>
    public byte[] Encrypt(ReadOnlySpan<byte> data, ReadOnlySpan<byte> associatedData = default, int paddingBoundary = 0)
    {
        // 1. Calculate and apply padding to obscure plaintext length
        int paddingLen = paddingBoundary > 0 ? (paddingBoundary - (data.Length % paddingBoundary)) % paddingBoundary : 0;
        Span<byte> plaintext = new byte[data.Length + paddingLen];
        data.CopyTo(plaintext);

        // 2. Generate a random 24-byte extended nonce
        byte[] nonce = RandomNumberGenerator.GetBytes(Algorithm.NonceSize);

        // 3. Perform AEAD encryption (NSec returns data as [Cipher][Tag])
        byte[] rawEncrypted = Algorithm.Encrypt(_key, nonce, associatedData, plaintext);

        // 4. Assemble the final packet structure
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        bw.Write(CurrentVersion);           // 1 byte
        bw.Write((ushort)paddingLen);       // 2 bytes
        bw.Write(nonce);                    // 24 bytes

        // Extract Tag from the end and move it before the Ciphertext for streaming-friendly parsing
        ReadOnlySpan<byte> tag = rawEncrypted.AsSpan(^TagSize);
        ReadOnlySpan<byte> cipher = rawEncrypted.AsSpan(0, rawEncrypted.Length - TagSize);

        bw.Write(tag);                      // 16 байт
        bw.Write(cipher);                   // Variable length

        return ms.ToArray();
    }

    /// <summary>
    /// Parses an encrypted packet, verifies its integrity, and returns the original plaintext.
    /// </summary>
    /// <param name="packet">The full encrypted packet as returned by the <see cref="Encrypt"/> method.</param>
    /// <param name="associatedData">The same non-secret data used during encryption.</param>
    /// <returns>The original decrypted bytes without padding.</returns>
    /// <exception cref="CryptographicException">Thrown if authentication fails or the packet format is invalid.</exception>
    public byte[] Decrypt(ReadOnlySpan<byte> packet, ReadOnlySpan<byte> associatedData = default)
    {
        int offset = 0;
        ushort paddingLen = 0;

        // 1. Minimum size validation: Ver(1) + Pad(2) + Nonce(24) + Tag(16)
        if (packet.Length < 1 + 2 + NonceSize + TagSize)
            throw new CryptographicException("Packet is too small.");

        // 2. Protocol version check
        if (packet[offset] != CurrentVersion)
            throw new CryptographicException("Version mismatch.");
        offset += 1;

        // 3. Extract metadata and cryptographic components
        paddingLen = BinaryPrimitives.ReadUInt16LittleEndian(packet.Slice(offset, 2));
        offset += 2;

        // 4. Read Nonce (24 bytes)
        if (packet.Length < offset + NonceSize + TagSize)
            throw new CryptographicException("Invalid packet structure.");

        ReadOnlySpan<byte> nonce = packet.Slice(offset, NonceSize);
        offset += NonceSize;

        // 5. Read Tag (16) and Cipher (other)
        ReadOnlySpan<byte> tag = packet.Slice(offset, TagSize);
        ReadOnlySpan<byte> cipher = packet.Slice(offset + TagSize);

        // 6. Reconstruct the format expected by NSec/libsodium: [Cipher][Tag]
        byte[] rawForNSec = new byte[cipher.Length + TagSize];
        cipher.CopyTo(rawForNSec);
        tag.CopyTo(rawForNSec.AsSpan(cipher.Length));

        // 7. Decrypt and verify Poly1305 MAC
        byte[]? decrypted = Algorithm.Decrypt(_key, nonce, associatedData, rawForNSec);

        if (decrypted == null)
        {
            throw new CryptographicException("Decryption/Authentication failed (MAC mismatch).");
        }

        // 8. Remove padding based on metadata
        if (paddingLen > 0)
        {
            if (paddingLen > decrypted.Length)
                throw new CryptographicException("Invalid padding length in packet.");

            // Return slice without padding
            return decrypted[..^paddingLen];
        }

        return decrypted;
    }

    /// <summary>
    /// Safely clears the cryptographic key from memory.
    /// </summary>
    public void Dispose() => _key?.Dispose();
}
