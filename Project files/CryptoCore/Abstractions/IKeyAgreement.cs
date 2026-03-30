namespace CryptoCore.Abstractions;

/// <summary>
/// Defines the contract for Key Agreement protocols (e.g., Diffie-Hellman).
/// This interface enables two parties to establish a shared secret over an insecure channel.
/// </summary>
/// <remarks>
/// The resulting shared secret is a raw cryptographic value that should be processed 
/// through a Key Derivation Function (KDF) before being used for encryption.
/// </remarks>
public interface IKeyAgreement : IDisposable
{
    /// <summary>
    /// Gets the local public key to be shared with the remote party.
    /// </summary>
    /// <value>A byte array representing the public point on the elliptic curve.</value>
    byte[] PublicKey { get; }

    /// <summary>
    /// Computes the shared secret using the local private key and the remote party's public key.
    /// </summary>
    /// <param name="otherPartyPublicKey">The public key received from the other participant.</param>
    /// <returns>A byte array containing the derived shared secret.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided public key is invalid or malformed.</exception>
    byte[] DeriveSharedSecret(ReadOnlySpan<byte> otherPartyPublicKey);
}
