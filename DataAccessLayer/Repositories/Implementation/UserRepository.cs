using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories.Implementation;

/// <summary>
/// Repository implementation for User entity data access operations.
/// Handles all database interactions related to user authentication and profile management.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the UserRepository.
    /// </summary>
    /// <param name="context">The application database context.</param>
    public UserRepository(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Retrieves a user by email address.
    /// </summary>
    /// <param name="email">The email address to search for.</param>
    /// <returns>The user if found; otherwise null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when email is null or whitespace.</exception>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentNullException(nameof(email), "Email cannot be null or whitespace.");
        }

        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email.ToLower());
    }

    /// <summary>
    /// Retrieves a user by unique identifier.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The user if found; otherwise null.</returns>
    /// <exception cref="ArgumentException">Thrown when userId is an empty GUID.</exception>
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User ID cannot be empty.", nameof(userId));
        }

        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>
    /// Creates a new user account in the database.
    /// </summary>
    /// <param name="user">The user entity to create.</param>
    /// <returns>The created user with database-generated values.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public async Task<User> CreateUserAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User cannot be null.");
        }

        user.Email = user.Email.ToLower();
        user.CreatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Updates an existing user's profile information.
    /// </summary>
    /// <param name="user">The user entity with updated values.</param>
    /// <returns>The updated user entity.</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null.</exception>
    public async Task<User> UpdateUserAsync(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user), "User cannot be null.");
        }

        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Checks if a user exists by email address.
    /// </summary>
    /// <param name="email">The email address to check.</param>
    /// <returns>True if user exists; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when email is null or whitespace.</exception>
    public async Task<bool> UserExistsByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentNullException(nameof(email), "Email cannot be null or whitespace.");
        }

        return await _context.Users.AnyAsync(u => u.Email == email.ToLower());
    }
}
