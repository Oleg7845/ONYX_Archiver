using CryptoCore.Abstractions;

namespace CryptoCore.Cryptography.Keys.Ed25519;

/// <summary>
/// A high-level provider for Ed25519 digital signatures.
/// Bridges the <see cref="Ed25519KeyContext"/> with the application's signing and verification abstractions.
/// </summary>
/// <remarks>
/// This provider allows for both signing data (using the internal private key) 
/// and verifying signatures (using either the internal or an external public key).
/// </remarks>
public sealed class Ed25519Provider : ISigner, IVerifier, IDisposable
{
    private readonly Ed25519KeyContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="Ed25519Provider"/> using a specific key context.
    /// </summary>
    /// <param name="context">The cryptographic context containing the Ed25519 key pair.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided context is null.</exception>
    public Ed25519Provider(Ed25519KeyContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Retrieves the public key associated with the current context.
    /// </summary>
    /// <returns>A 32-byte array representing the Ed25519 public key.</returns>
    public byte[] GetPublicKey()
    {
        return _context.PublicKey;
    }

    /// <summary>
    /// Signs the provided data using the private key in the current context.
    /// </summary>
    /// <param name="data">The message or hash to be signed.</param>
    /// <returns>A 64-byte EdDSA signature.</returns>
    public byte[] Sign(ReadOnlySpan<byte> data)
    {
        return _context.Sign(data);
    }

    /// <summary>
    /// Verifies a digital signature against the provided data using the current context's public key.
    /// </summary>
    /// <param name="data">The data that was supposedly signed.</param>
    /// <param name="signature">The 64-byte signature to check.</param>
    /// <returns>True if the signature is valid for this specific public key; otherwise, false.</returns>
    public bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        return _context.Verify(data, signature);
    }

    /// <summary>
    /// Verifies a signature from an external source using their public key.
    /// Implements the <see cref="IVerifier"/> interface requirement.
    /// </summary>
    /// <param name="data">The signed data.</param>
    /// <param name="signature">The signature bytes.</param>
    /// <param name="publicKey">The public key of the signer.</param>
    /// <returns>True if the signature is authentic.</returns>
    public bool VerifyRemote(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        return Verify(data, signature, publicKey);
    }

    /// <summary>
    /// Provides a static, stateless verification method for Ed25519 signatures.
    /// This is useful when you have a public key but do not need to maintain a full key context.
    /// </summary>
    /// <param name="data">The signed data.</param>
    /// <param name="signature">The 64-byte signature.</param>
    /// <param name="publicKey">The 32-byte public key to verify against.</param>
    /// <returns>True if the signature is cryptographically valid.</returns>
    public static bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey)
    {
        return Ed25519KeyContext.VerifyRemote(data, signature, publicKey);
    }

    /// <summary>
    /// Disposes the underlying key context and wipes sensitive information.
    /// </summary>
    public void Dispose()
    {
        _context?.Dispose();
    }
}
