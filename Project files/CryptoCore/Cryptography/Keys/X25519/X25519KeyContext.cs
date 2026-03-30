using CryptoCore.Abstractions;
using NSec.Cryptography;
using System.Security.Cryptography;

namespace CryptoCore.Cryptography.Keys.X25519;

/// <summary>
/// Manages an X25519 key pair and provides Elliptic Curve Diffie-Hellman (ECDH) operations.
/// This context is used to establish a shared secret between two peers over an insecure channel.
/// </summary>
/// <remarks>
/// X25519 is high-speed and resistant to many side-channel attacks. 
/// The shared secret generated here should be further processed by a KDF (like HKDF) before use.
/// </remarks>
public sealed class X25519KeyContext : IKeyAgreement, IDisposable
{
    private static readonly KeyAgreementAlgorithm Algorithm = KeyAgreementAlgorithm.X25519;
    private readonly Key _privateKey;

    /// <summary>
    /// Gets the raw 32-byte public key associated with this context.
    /// This key can be safely shared with anyone.
    /// </summary>
    public byte[] PublicKey { get; }

    /// <summary>
    /// Initializes a new <see cref="X25519KeyContext"/> by generating a fresh ephemeral key pair.
    /// </summary>
    public X25519KeyContext()
    {
        // Allow export to enable encrypted persistence of the private key
        var creationParams = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        _privateKey = Key.Create(Algorithm, creationParams);
        PublicKey = _privateKey.Export(KeyBlobFormat.RawPublicKey);
    }

    /// <summary>
    /// Initializes a new <see cref="X25519KeyContext"/> by importing an existing private key.
    /// </summary>
    /// <param name="privateKeyBytes">The raw 32-byte private key.</param>
    public X25519KeyContext(ReadOnlySpan<byte> privateKeyBytes)
    {
        var importParams = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        _privateKey = Key.Import(Algorithm, privateKeyBytes, KeyBlobFormat.RawPrivateKey, importParams);
        PublicKey = _privateKey.Export(KeyBlobFormat.RawPublicKey);
    }

    /// <summary>
    /// Performs the Key Agreement (Diffie-Hellman) to compute a Shared Secret.
    /// </summary>
    /// <param name="otherPartyPublicKey">The 32-byte public key received from the other party.</param>
    /// <returns>A 32-byte shared secret (the X-coordinate of the resulting point on the curve).</returns>
    /// <remarks>
    /// The resulting secret is high-entropy but should not be used directly as an encryption key.
    /// It must be passed through a Key Derivation Function (KDF).
    /// </remarks>
    public byte[] DeriveSharedSecret(ReadOnlySpan<byte> otherPartyPublicKey)
    {
        // Import the remote public key into the cryptographic engine
        var otherPublic = NSec.Cryptography.PublicKey.Import(Algorithm, otherPartyPublicKey, KeyBlobFormat.RawPublicKey);

        var secretParams = new SharedSecretCreationParameters
        {
            ExportPolicy = KeyExportPolicies.AllowPlaintextExport
        };

        // Compute the Elliptic Curve point multiplication
        using var sharedSecret = Algorithm.Agree(_privateKey, otherPublic, secretParams);

        // Export for subsequent input into HKDF
        return sharedSecret.Export(SharedSecretBlobFormat.RawSharedSecret);
    }

    /// <summary>
    /// Exports the private key in an encrypted format to ensure it is never handled in plaintext outside this method.
    /// </summary>
    /// <param name="encrypter">The encryption service used to protect the raw key bytes.</param>
    /// <returns>A byte array containing the encrypted private key.</returns>
    public byte[] ExportEncrypted(IEncrypter encrypter)
    {
        byte[] rawKey = _privateKey.Export(KeyBlobFormat.RawPrivateKey);
        try
        {
            return encrypter.Encrypt(rawKey);
        }
        finally
        {
            // Securely wipe the plaintext key from memory after encryption
            CryptographicOperations.ZeroMemory(rawKey);
        }
    }

    /// <summary>
    /// Decrypts and restores an <see cref="X25519KeyContext"/> from encrypted private key data.
    /// </summary>
    /// <param name="encryptedData">The encrypted private key bytes.</param>
    /// <param name="decrypter">The decryption service to unlock the key.</param>
    /// <returns>A restored key context.</returns>
    public static X25519KeyContext ImportEncrypted(byte[] encryptedData, IDecrypter decrypter)
    {
        byte[] rawKey = decrypter.Decrypt(encryptedData);
        try
        {
            return new X25519KeyContext(rawKey);
        }
        finally
        {
            // Securely wipe the plaintext key from memory after import
            CryptographicOperations.ZeroMemory(rawKey);
        }
    }

    /// <summary>
    /// Securely disposes the private key and clears sensitive data.
    /// </summary>
    public void Dispose()
    {
        _privateKey?.Dispose();
    }
}
