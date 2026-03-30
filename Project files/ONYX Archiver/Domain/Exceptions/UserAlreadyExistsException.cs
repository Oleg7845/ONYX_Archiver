namespace OnyxArchiver.Domain.Exceptions;

/// <summary>
/// Thrown during the registration or account creation process if the requested 
/// username is already associated with another local account in the database.
/// </summary>
/// <remarks>
/// This exception prevents duplicate identity records, which is essential because 
/// usernames are often used as unique seeds for local storage paths and identity lookups.
/// </remarks>
public class UserAlreadyExistsException : DomainException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserAlreadyExistsException"/> class.
    /// </summary>
    /// <param name="username">The username that caused the naming conflict.</param>
    public UserAlreadyExistsException(string username)
        : base(
            userMessage: $"The username '{username}' is already taken. Please choose a different one.",
            technicalMessage: $"Registration failed: Unique constraint violation for username '{username}'.")
    {
    }
}