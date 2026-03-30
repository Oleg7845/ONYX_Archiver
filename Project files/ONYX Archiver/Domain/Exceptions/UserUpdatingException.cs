namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when the application fails to update user profile information, 
/// cryptographic identity keys, or security settings in the local database.
/// </summary>
/// <remarks>
/// This exception is critical during security-sensitive operations such as:
/// 1. Password changes (updating the Argon2id hash).
/// 2. Key rotation (updating the encrypted Private Keys).
/// 3. Security parameter upgrades (updating the KeyDescriptor).
/// </remarks>
public class UserUpdatingException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserUpdatingException"/> class.
    /// </summary>
    /// <param name="username">The username of the account that failed to update.</param>
    public UserUpdatingException(string username)
        : base(userMessage: $"An error occurred while saving changes to the profile for '{username}'.")
    {
    }

    /// <summary>
    /// Initializes a new instance with a specific technical reason for the update failure.
    /// </summary>
    /// <param name="technicalMessage">Details like "Database disk image is malformed" or "Disk full while writing SQLite journal."</param>
    /// <param name="username">The username of the account.</param>
    public UserUpdatingException(string technicalMessage, string username)
        : base(
            userMessage: "Your profile settings could not be saved.",
            technicalMessage: $"User: {username} | Error: {technicalMessage}")
    {
    }
}