using NSec.Cryptography;
using System.Security.Cryptography;

namespace CryptoCore.Cryptography.Hashing;

/// <summary>
/// Implements the HMAC-based Extract-and-Expand Key Derivation Function (HKDF) using SHA-512.
/// HKDF is used to derive high-entropy cryptographic keys from various input keying materials.
/// </summary>
public static class Hkdf
{
    /// <summary> The underlying derivation algorithm based on SHA-512. </summary>
    private static readonly KeyDerivationAlgorithm Algorithm = KeyDerivationAlgorithm.HkdfSha512;

    /// <summary> Default context information string to prevent cross-protocol key reuse. </summary>
    private static readonly byte[] DefaultInfo = System.Text.Encoding.UTF8.GetBytes("CryptoCore.HKDF.SHA512.v1");

    /// <summary> Recommended size for the salt value (32 bytes). </summary>
    public const int DefaultSaltSize = 32;

    /// <summary>
    /// Derives a key and generates a new random salt if none is provided.
    /// </summary>
    /// <param name="inputKeyingMaterial">The source material (e.g., a shared secret or master password).</param>
    /// <param name="salt">Optional salt value. If null, a new random salt is generated.</param>
    /// <param name="info">Optional context-specific information to bind the key to a specific use case.</param>
    /// <param name="outputSize">The desired length of the derived key in bytes (default is 32).</param>
    /// <returns>A tuple containing the derived <c>Key</c> and the <c>Salt</c> used during derivation.</returns>
    public static (byte[] Key, byte[] Salt) DeriveKey(
        ReadOnlySpan<byte> inputKeyingMaterial,
        byte[]? salt = null,
        byte[]? info = null,
        int outputSize = 32)
    {
        byte[] finalSalt = salt ?? RandomNumberGenerator.GetBytes(DefaultSaltSize);
        byte[] finalInfo = info ?? DefaultInfo;
        byte[] derivedKey = Algorithm.DeriveBytes(inputKeyingMaterial, finalSalt, finalInfo, outputSize);

        return (derivedKey, finalSalt);
    }

    /// <summary>
    /// Derives a key using a specific salt. This is typically used during decryption or key reconstruction.
    /// </summary>
    /// <param name="inputKeyingMaterial">The source material.</param>
    /// <param name="salt">The salt that was used during the original key derivation.</param>
    /// <param name="info">Optional context-specific information.</param>
    /// <param name="outputSize">The desired length of the derived key in bytes.</param>
    /// <returns>A byte array containing the derived key.</returns>
    public static byte[] DeriveKey(
        ReadOnlySpan<byte> inputKeyingMaterial,
        ReadOnlySpan<byte> salt,
        byte[]? info = null,
        int outputSize = 32)
    {
        byte[] finalInfo = info ?? DefaultInfo;

        return Algorithm.DeriveBytes(inputKeyingMaterial, salt, finalInfo, outputSize);
    }

    /// <summary>
    /// Generates a cryptographically strong random salt of <see cref="DefaultSaltSize"/>.
    /// </summary>
    /// <returns>A random byte array.</returns>
    public static byte[] GenerateRandomSalt()
    {
        return RandomNumberGenerator.GetBytes(DefaultSaltSize);
    }
}
