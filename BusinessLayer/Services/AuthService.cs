using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Business.Services;

/// <summary>
/// Provides custom authentication and registration using BCrypt password hashing
/// and ASP.NET Core cookie authentication. Uses the custom database schema
/// (not ASP.NET Identity) aligned with <c>database-script.sql</c>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AuthService"/> class.
/// </remarks>
/// <param name="context">The EF Core database context.</param>
/// <param name="httpContextAccessor">The HTTP context accessor for signing in/out.</param>
public class AuthService(UserManager<ApplicationUser> userManager) : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    /// <summary>
    /// Authenticates a user by email and password using BCrypt verification.
    /// Signs in via a cookie on success.
    /// </summary>
    /// <param name="request">Login credentials including email, password and remember-me flag.</param>
    /// <returns><c>true</c> if authentication succeeded; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        // 1. Find active user by email
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return new LoginResult
            {
                Success = false,
                Errors = [AppConstants.InvalidCredentials],
            };

        // 2. Verify password with BCrypt
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return new LoginResult
            {
                Success = false,
                Errors = [AppConstants.InvalidCredentials],
            };

        // 3. Build claims identity and sign in
        var loginResult = new LoginResult
        {
            Success = true,
            User = user,
            Claims = await _userManager.GetClaimsAsync(user),
            Errors = [],
        };
        return loginResult;
    }

    /// <summary>
    /// Creates a new user account with a BCrypt-hashed password,
    /// then assigns the default Student role.
    /// </summary>
    /// <param name="request">Registration data including email, full name, and password.</param>
    /// <returns>
    /// <c>null</c> on success, or an error message string if registration fails
    /// (e.g., email already exists).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public async Task<string?> RegisterAsync(string email, string fullName, string password)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(fullName);
        ArgumentNullException.ThrowIfNull(password);

        // 1. Check for duplicate email
        var exists = await _userManager.FindByEmailAsync(email) != null;
        if (exists)
        {
            return "An account with this email address already exists.";
        }

        // 2. Hash password and create the user entity
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };

        // 3. Add user to store
        await _userManager.CreateAsync(user);    // Double hash

        // 4. Assign Student role and claims
        await _userManager.AddToRoleAsync(user, UserRole.Student.ToString());
        await _userManager.AddClaimsAsync(user,
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, UserRole.Student.ToString()),
        ]);

        return null; // null = success
    }

    /// <summary>
    /// Signs out the currently authenticated user by clearing the authentication cookie.
    /// </summary>
    /// <returns>A task representing the asynchronous sign-out operation.</returns>
    public async Task LogoutAsync()
    {
    }
}
