namespace CryptoCore.Abstractions;

/// <summary>
/// Defines the contract for cryptographic verification operations.
/// This interface is used to validate the integrity and authenticity of data 
/// by checking digital signatures against public keys.
/// </summary>
public interface IVerifier : IDisposable
{
    /// <summary>
    /// Verifies a digital signature against the provided data using the 
    /// public key associated with the current instance.
    /// </summary>
    /// <param name="data">The original data that was signed.</param>
    /// <param name="signature">The digital signature to be validated.</param>
    /// <returns><c>true</c> if the signature is valid; otherwise, <c>false</c>.</returns>
    bool Verify(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature);

    /// <summary>
    /// Verifies a digital signature using an explicitly provided external public key.
    /// </summary>
    /// <remarks>
    /// This method is essential for verifying archives created by other users 
    /// whose public keys are known but not stored within the local context.
    /// </remarks>
    /// <param name="data">The original data that was signed.</param>
    /// <param name="signature">The digital signature to be validated.</param>
    /// <param name="publicKey">The public key of the entity that signed the data.</param>
    /// <returns><c>true</c> if the signature is valid for the given public key; otherwise, <c>false</c>.</returns>
    bool VerifyRemote(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> publicKey);
}
