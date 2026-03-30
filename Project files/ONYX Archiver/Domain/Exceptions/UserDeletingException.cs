namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when the application fails to fully decommission a local user account.
/// This is a critical error as it may leave sensitive cryptographic material 
/// or orphaned identity records on the local machine.
/// </summary>
/// <remarks>
/// Account deletion failures typically occur due to:
/// 1. Active database transactions or locks on the User table.
/// 2. Filesystem permission issues when attempting to delete the user's secure vault folder.
/// 3. Referential integrity constraints (e.g., if archives or peer logs are still linked to this user).
/// </remarks>
public class UserDeletingException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserDeletingException"/> class.
    /// </summary>
    /// <param name="username">The name of the user whose account deletion could not be completed.</param>
    public UserDeletingException(string username)
        : base(userMessage: $"An error occurred while deleting the account for '{username}'.")
    {
    }

    /// <summary>
    /// Initializes a new instance with specific technical details for debugging cleanup failures.
    /// </summary>
    /// <param name="technicalMessage">Details like "Access denied to %AppData%/OnyxArchiver/Keys" or "SQLite constraint violation."</param>
    /// <param name="username">The name of the user.</param>
    public UserDeletingException(string technicalMessage, string username)
        : base(
            userMessage: $"The account for '{username}' could not be fully removed.",
            technicalMessage: $"User: {username} | Error: {technicalMessage}")
    {
    }
}