namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Defines the contract for user authentication, registration, and session management.
/// Orchestrates the transformation of raw credentials into secure, active cryptographic sessions.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Gets a value indicating whether a valid cryptographic session is currently active.
    /// </summary>
    bool IsLoggedIn { get; }

    /// <summary>
    /// Gets the username of the currently authenticated user.
    /// Returns <see cref="string.Empty"/> if no session is active.
    /// </summary>
    string Username { get; }

    /// <summary>
    /// Authenticates a user by verifying their password against the stored Argon2id hash.
    /// Upon success, it decrypts the user's private keys and initializes the <see cref="IUserVaultService"/>.
    /// </summary>
    /// <param name="username">The unique identifier of the local account.</param>
    /// <param name="password">The master password used to unlock the encrypted identity keys.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous login process.</returns>
    /// <exception cref="UserNotFoundException">Thrown if the username does not exist in the local database.</exception>
    /// <exception cref="InvalidPasswordException">Thrown if the provided password does not match the stored hash.</exception>
    Task Login(string username, string password);

    /// <summary>
    /// Provisions a new user account, generating a unique X25519/Ed25519 key pair.
    /// The private keys are then encrypted (wrapped) using a key derived from the provided password.
    /// </summary>
    /// <param name="username">The desired unique username for the local vault.</param>
    /// <param name="password">The master password that will serve as the root of trust for the account.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous registration and key generation.</returns>
    /// <exception cref="UserAlreadyExistsException">Thrown if the username is already taken on this machine.</exception>
    Task Registration(string username, string password);

    /// <summary>
    /// Terminates the current session and ensures all sensitive keying material is securely purged from memory.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous logout and cleanup process.</returns>
    Task Logout();
}