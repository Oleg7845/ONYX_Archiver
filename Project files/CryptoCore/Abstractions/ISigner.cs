namespace CryptoCore.Abstractions;

/// <summary>
/// Defines the contract for cryptographic signing operations.
/// This interface ensures that any implementing service can generate digital signatures 
/// to guarantee data authenticity and non-repudiation.
/// </summary>
public interface ISigner : IDisposable
{
    /// <summary>
    /// Computes a digital signature for the provided data.
    /// </summary>
    /// <param name="data">The data buffer to be signed. Using <see cref="ReadOnlySpan{T}"/> 
    /// provides high-performance, memory-efficient access to the underlying bytes.</param>
    /// <returns>A byte array containing the digital signature.</returns>
    byte[] Sign(ReadOnlySpan<byte> data);

    /// <summary>
    /// Retrieves the public key associated with this signer's private key.
    /// </summary>
    /// <remarks>
    /// This public key is required by a verifier to validate the signatures produced by this instance.
    /// </remarks>
    /// <returns>A byte array representing the public key.</returns>
    byte[] GetPublicKey();
}
