namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when the cryptographic session context is invalid, missing, or corrupted.
/// This typically indicates that the user's master keys are not properly loaded into memory,
/// or the secure memory handle has been disposed of.
/// </summary>
public class EncryptionContextException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EncryptionContextException"/> class.
    /// </summary>
    /// <param name="userMessage">Friendly message like "Security session expired. Please log in again."</param>
    /// <param name="technicalMessage">Detailed info for logs, such as "Vault key handle is null" or "HKDF derivation failed."</param>
    public EncryptionContextException(string? userMessage = null, string? technicalMessage = null)
        : base(
            userMessage ?? "Your security session is no longer valid.",
            technicalMessage ?? "Encryption context was accessed while in an uninitialized state.")
    {
    }
}