using NSec.Cryptography;
using CryptoCore.Abstractions;
using System.Security.Cryptography;
using CryptoCore.Formatting.Pem;

namespace CryptoCore.Cryptography.Keys.Ed25519;

/// <summary>
/// Manages an Ed25519 key pair for creating and verifying Edwards-curve Digital Signatures (EdDSA).
/// Ed25519 is designed for high security, collision resistance, and high performance.
/// Implements <see cref="IDisposable"/> to ensure sensitive private key material is wiped from memory.
/// </summary>
public sealed class Ed25519KeyContext : IDisposable
{
    // Ed25519 is chosen for its deterministic nature (no need for a high-quality RNG during signing).
    private static readonly SignatureAlgorithm Algorithm = SignatureAlgorithm.Ed25519;

    // The underlying NSec Key object which handles secure memory allocations.
    private readonly Key _privateKey;

    /// <summary>
    /// Gets the raw 32-byte public key used for signature verification.
    /// This property is cached during initialization for performance.
    /// </summary>
    public byte[] PublicKey { get; }

    /// <summary>
    /// Initializes a new <see cref="Ed25519KeyContext"/> by generating a fresh signing key pair.
    /// </summary>
    public Ed25519KeyContext()
    {
        // ExportPolicy.AllowPlaintextExport is required to enable PEM and Encrypted exports later.
        var creationParams = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        _privateKey = Key.Create(Algorithm, creationParams);

        // Pre-calculate and cache the public key.
        PublicKey = _privateKey.Export(KeyBlobFormat.RawPublicKey);
    }

    /// <summary>
    /// Initializes a new <see cref="Ed25519KeyContext"/> by importing an existing raw private key.
    /// </summary>
    /// <param name="privateKeyBytes">The raw 32-byte private key (seed).</param>
    public Ed25519KeyContext(ReadOnlySpan<byte> privateKeyBytes)
    {
        var importParams = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        _privateKey = Key.Import(Algorithm, privateKeyBytes, KeyBlobFormat.RawPrivateKey, importParams);
        PublicKey = _privateKey.Export(KeyBlobFormat.RawPublicKey);
    }

    /// <summary>
    /// Internal constructor for creating a context from a pre-imported NSec <see cref="Key"/>.
    /// </summary>
    private Ed25519KeyContext(Key key)
    {
        _privateKey = key ?? throw new ArgumentNullException(nameof(key));
        PublicKey = _privateKey.Export(KeyBlobFormat.RawPublicKey);
    }

    /// <summary>
    /// Generates a digital signature for the provided data using the Ed25519 private key.
    /// </summary>
    /// <param name="data">The message or hash to sign.</param>
    /// <returns>A 64-byte EdDSA signature.</returns>
    public byte[] Sign(ReadOnlySpan<byte> data)
    {
        return Algorithm.Sign(_privateKey, data);
    }

    /// <summary>
    /// Verifies a signature against the provided data using this instance's public key.
    /// </summary>
    /// <param name="data">The original data that was signed.</param>
    /// <param name="signature">The 64-byte signature to verify.</param>
    /// <returns>True if the signature is valid; otherwise, false.</returns>
    public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        // Re-import the cached public key into an NSec-compatible object.
        var pubKey = NSec.Cryptography.PublicKey.Import(Algorithm, PublicKey, KeyBlobFormat.RawPublicKey);
        return Algorithm.Verify(pubKey, data, signature);
    }

    /// <summary>
    /// Static utility to verify a signature using an external public key without instantiating a full context.
    /// </summary>
    /// <param name="data">The signed data.</param>
    /// <param name="signature">The signature bytes.</param>
    /// <param name="remotePublicKey">The 32-byte public key of the signer.</param>
    /// <returns>True if the signature matches the data and public key.</returns>
    public static bool VerifyRemote(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> remotePublicKey)
    {
        var pubKey = NSec.Cryptography.PublicKey.Import(Algorithm, remotePublicKey, KeyBlobFormat.RawPublicKey);
        return Algorithm.Verify(pubKey, data, signature);
    }

    /// <summary>
    /// Exports the private key material to a protected format using a custom <see cref="IEncrypter"/>.
    /// </summary>
    /// <param name="encrypter">The encryption strategy to use (e.g., XChaCha20-Poly1305).</param>
    /// <returns>A byte array containing the encrypted key.</returns>
    public byte[] ExportEncrypted(IEncrypter encrypter)
    {
        // Temporarily export to raw bytes for encryption.
        byte[] rawKey = _privateKey.Export(KeyBlobFormat.RawPrivateKey);
        try
        {
            return encrypter.Encrypt(rawKey);
        }
        finally
        {
            // Critical: Immediate sensitive data wipe to prevent memory scraping.
            CryptographicOperations.ZeroMemory(rawKey);
        }
    }

    /// <summary>
    /// Restores a signing context from an encrypted blob.
    /// </summary>
    /// <param name="encryptedData">The encrypted private key blob.</param>
    /// <param name="decrypter">The decryption strategy to unlock the key.</param>
    /// <returns>A new <see cref="Ed25519KeyContext"/> instance.</returns>
    public static Ed25519KeyContext ImportEncrypted(byte[] encryptedData, IDecrypter decrypter)
    {
        byte[] rawKey = decrypter.Decrypt(encryptedData);
        try
        {
            return new Ed25519KeyContext(rawKey);
        }
        finally
        {
            // Critical: Wipe the temporary plaintext buffer after the key is imported.
            CryptographicOperations.ZeroMemory(rawKey);
        }
    }

    /// <summary>
    /// Persists the private key to a PEM-formatted file, optionally encrypted with a password.
    /// </summary>
    /// <param name="pemFilePath">Output file path.</param>
    /// <param name="password">Optional password for PBES2 protection.</param>
    public void ExportPrivateKeyToPem(string pemFilePath, ReadOnlySpan<char> password = default)
    {
        PemEd25519IO.SaveEd25519PrivateKeyPem(
            _privateKey,
            pemFilePath,
            password);
    }

    /// <summary>
    /// Loads a private key context from a PEM-formatted file.
    /// </summary>
    /// <param name="pemFilePath">Source file path.</param>
    /// <param name="password">The password used for decryption (if the PEM is encrypted).</param>
    /// <returns>A restored signing context.</returns>
    public static Ed25519KeyContext ImportPrivateKeyFromPem(string pemFilePath, ReadOnlySpan<char> password = default)
    {
        var key = PemEd25519IO.LoadEd25519PrivateKeyPem(pemFilePath, password);
        return new Ed25519KeyContext(key);
    }

    /// <summary>
    /// Disposes of the underlying <see cref="Key"/> object, securely erasing the private key material.
    /// </summary>
    public void Dispose() => _privateKey?.Dispose();
}