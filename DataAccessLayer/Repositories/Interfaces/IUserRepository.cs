using DataAccessLayer.Entities;
using System;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Interfaces;

/// <summary>
/// Interface defining data access operations for User entities.
/// Provides methods for authentication and user profile management.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetUserByEmailAsync(string email);

    /// <summary>
    /// Retrieves a user by unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetUserByIdAsync(Guid userId);

    /// <summary>
    /// Creates a new user account in the database.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <returns>The created user with database-generated values.</returns>
    Task<User> CreateUserAsync(User user);

    /// <summary>
    /// Updates an existing user's profile information.
    /// </summary>
    /// <param name="user">The user entity with updated values.</param>
    /// <returns>The updated user entity.</returns>
    Task<User> UpdateUserAsync(User user);

    /// <summary>
    /// Checks if a user exists by email address.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns>True if user exists; otherwise false.</returns>
    Task<bool> UserExistsByEmailAsync(string email);
}
