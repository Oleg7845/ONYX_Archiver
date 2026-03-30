namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when an internal error occurs within the underlying cryptographic provider.
/// Indicates failures during the mathematical execution of encryption, decryption, or signing algorithms.
/// </summary>
/// <remarks>
/// This exception is a critical security signal. It is typically raised if the 
/// AEAD (Authenticated Encryption with Associated Data) tag verification fails, 
/// which strongly suggests either hardware-level data corruption or an intentional tampering attempt.
/// </remarks>
public class EncryptionProviderException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionProviderException"/> class.
    /// </summary>
    /// <param name="userMessage">Friendly message like "The archive appears to be corrupted or tampered with."</param>
    /// <param name="technicalMessage">Detailed info like "Poly1305 MAC mismatch" or "Decryption buffer overflow."</param>
    public EncryptionProviderException(string? userMessage = null, string? technicalMessage = null)
        : base(
            userMessage ?? "A cryptographic error occurred while processing the data.",
            technicalMessage ?? "Internal cryptographic provider failure.")
    {
    }
}