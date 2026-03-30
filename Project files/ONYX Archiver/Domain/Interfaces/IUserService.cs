namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Provides administrative actions for the currently authenticated user account.
/// Handles high-sensitivity operations such as credential rotation and total account decommissioning.
/// </summary>
/// <remarks>
/// All methods in this interface require the current password as a multi-factor 
/// authorization step, ensuring that even if a session is hijacked, the underlying 
/// identity remains protected.
/// </remarks>
public interface IUserService
{
    /// <summary>
    /// Performs a secure, atomic password rotation. 
    /// Verifies the current credentials, derives a new master key using Argon2id, 
    /// and re-encrypts the user's private identity keys (X25519/Ed25519) with the new key.
    /// </summary>
    /// <param name="currentPassword">The user's existing password for identity verification.</param>
    /// <param name="newPassword">The new master secret to be established.</param>
    /// <returns>A <see cref="Task"/> representing the completion of the rotation.</returns>
    /// <exception cref="InvalidPasswordException">Thrown if the current password check fails.</exception>
    /// <exception cref="UserUpdatingException">Thrown if the re-encryption or database persistence fails.</exception>
    Task UpdateUserPasswordAsync(string currentPassword, string newPassword);

    /// <summary>
    /// Permanently and securely deletes the user's profile from the local machine.
    /// This includes all associated peers, cryptographic keys, and metadata.
    /// </summary>
    /// <remarks>
    /// This is a "Nuclear" operation. Once completed, all archives encrypted for this user 
    /// by peers will become unreadable unless a backup of the identity keys exists elsewhere.
    /// </remarks>
    /// <param name="password">The user's password to confirm the finality of the deletion request.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous removal process.</returns>
    /// <exception cref="InvalidPasswordException">Thrown if the confirmation password does not match.</exception>
    /// <exception cref="UserDeletingException">Thrown if the database or filesystem cleanup fails.</exception>
    Task DeleteUserAsync(string password);
}