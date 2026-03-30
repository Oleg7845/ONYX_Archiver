using OnyxArchiver.Domain.Entities;

namespace OnyxArchiver.Domain.Interfaces;

/// <summary>
/// Defines the contract for persistence operations related to local user accounts.
/// Manages the physical storage and retrieval of <see cref="UserEntity"/> objects 
/// which hold the master Argon2id hashes and encrypted X25519/Ed25519 private keys.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Persists a new user account to the local database.
    /// This is the final step of the registration process.
    /// </summary>
    /// <param name="user">The user entity containing hashed credentials and wrapped keys.</param>
    /// <returns>A task representing the asynchronous operation, returning true if successful.</returns>
    Task<bool> AddAsync(UserEntity user);

    /// <summary>
    /// Retrieves a user profile by its primary database identifier.
    /// Useful for internal session management after a user has already logged in.
    /// </summary>
    Task<UserEntity?> GetByIdAsync(int id);

    /// <summary>
    /// Retrieves a user profile by their unique username. 
    /// This is the primary entry point for the authentication (Login) process.
    /// </summary>
    /// <param name="username">The case-sensitive or normalized username to look up.</param>
    /// <returns>The <see cref="UserEntity"/> if found; otherwise, null.</returns>
    Task<UserEntity?> GetByUsernameAsync(string username);

    /// <summary>
    /// Updates an existing user's information, such as rotated keys, 
    /// updated password hashes, or security parameter changes.
    /// </summary>
    /// <param name="user">The modified user entity to be persisted.</param>
    /// <returns>True if the update was successful and the record existed.</returns>
    Task<bool> UpdateAsync(UserEntity user);

    /// <summary>
    /// Permanently removes a user account and its associated master keys from the local system.
    /// </summary>
    /// <param name="id">The primary key of the user to be deleted.</param>
    /// <returns>True if the user was found and deleted.</returns>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Checks if a specific username is already registered in the local vault. 
    /// Used as a validation gate during the account creation phase.
    /// </summary>
    /// <param name="username">The username to verify.</param>
    /// <returns>True if the username is already in use.</returns>
    Task<bool> ExistsAsync(string username);
}