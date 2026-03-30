namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when a user provides a password that does not match the stored Argon2id hash 
/// or results in a derived key that fails to decrypt the protected identity keys.
/// </summary>
public class InvalidPasswordException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPasswordException"/> class 
    /// with a predefined, localized user-friendly error message.
    /// </summary>
    /// <remarks>
    /// By providing a constant user message, we ensure that authentication errors 
    /// remain consistent across the entire application UI.
    /// </remarks>
    public InvalidPasswordException()
        : base(userMessage: "The password provided is incorrect. Please try again.")
    {
    }

    /// <summary>
    /// Overload for cases where more specific technical information is required for logging.
    /// </summary>
    /// <param name="technicalMessage">Details like "Argon2id hash mismatch" or "AEAD tag mismatch on PrivateKey decryption."</param>
    public InvalidPasswordException(string technicalMessage)
        : base(userMessage: "The password provided is incorrect.", technicalMessage: technicalMessage)
    {
    }
}