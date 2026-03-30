using Microsoft.EntityFrameworkCore;
using OnyxArchiver.Domain.Entities;
using OnyxArchiver.Domain.Interfaces;

namespace OnyxArchiver.Infrastructure.Repositories;

/// <summary>
/// Provides an implementation of the <see cref="IUserRepository"/> interface for managing 
/// local user profiles and their security credentials.
/// </summary>
/// <remarks>
/// This repository handles sensitive user data. It uses short-lived database contexts 
/// to maintain high responsiveness and reliability in a desktop environment.
/// </remarks>
public class UserRepository : IUserRepository
{
    private readonly IDbContextFactory _contextFactory;

    public UserRepository(IDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Creates a new user profile in the database.
    /// </summary>
    /// <param name="user">The user entity containing profile details and encrypted keys.</param>
    /// <returns><c>true</c> if the user was successfully created.</returns>
    public async Task<bool> AddAsync(UserEntity user)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
            return true;
        }
    }

    /// <summary>
    /// Retrieves a user profile by its primary database identifier.
    /// </summary>
    /// <param name="id">The internal database ID.</param>
    /// <returns>The <see cref="UserEntity"/> or null if not found.</returns>
    public async Task<UserEntity?> GetByIdAsync(int id)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            return await context.Users.FindAsync(id);
        }
    }

    /// <summary>
    /// Finds a user profile by their unique username.
    /// Uses <c>AsNoTracking()</c> to optimize performance for authentication checks.
    /// </summary>
    /// <param name="username">The unique username to search for.</param>
    /// <returns>The matching <see cref="UserEntity"/> or null.</returns>
    public async Task<UserEntity?> GetByUsernameAsync(string username)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            return await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username);
        }
    }

    /// <summary>
    /// Updates an existing user's profile information, such as updated encrypted master keys.
    /// </summary>
    /// <param name="user">The user entity with updated information.</param>
    /// <returns><c>true</c> if the update was successful.</returns>
    public async Task<bool> UpdateAsync(UserEntity user)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            context.Users.Update(user);
            await context.SaveChangesAsync();
            return true;
        }
    }

    /// <summary>
    /// Permanently removes a user profile from the local database.
    /// </summary>
    /// <param name="id">The ID of the user to delete.</param>
    /// <returns><c>true</c> if the user was found and deleted.</returns>
    public async Task<bool> DeleteAsync(int id)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            var user = await context.Users.FindAsync(id);
            if (user != null)
            {
                context.Users.Remove(user);
                await context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Checks if a username is already taken in the local database.
    /// Essential for the registration/first-setup workflow.
    /// </summary>
    /// <param name="username">The username to verify.</param>
    /// <returns><c>true</c> if the user already exists.</returns>
    public async Task<bool> ExistsAsync(string username)
    {
        using (var context = _contextFactory.CreateDbContext())
        {
            return await context.Users.AnyAsync(u => u.Username == username);
        }
    }
}
