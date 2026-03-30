namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown when an operation is attempted on a username that cannot be located in the local database.
/// Primarily used during the login phase to distinguish between a missing identity and an incorrect password.
/// </summary>
/// <remarks>
/// This exception is essential for local vault management. It allows the UI to suggest 
/// "Registering" a new account if the user types a name that has no corresponding 
/// cryptographic keys or database records.
/// </remarks>
public class UserNotFoundException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundException"/> class.
    /// </summary>
    /// <param name="username">The username that was searched for but not found in the local repository.</param>
    public UserNotFoundException(string username)
        : base(
            userMessage: $"No local account found for '{username}'.",
            technicalMessage: $"Identity lookup failed: Username '{username}' does not exist in UserEntity table.")
    {
    }

    /// <summary>
    /// Overload for cases where the specific username might be sensitive or unknown.
    /// </summary>
    public UserNotFoundException()
        : base(userMessage: "The requested user account was not found.") { }
}