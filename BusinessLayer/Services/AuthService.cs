using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
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
    /// Resolves a Google OAuth login by finding, linking, or creating a user account.
    /// Handles three cases: existing Google-linked account, existing email account (link it),
    /// or brand-new user (auto-register). Email is marked confirmed in all cases.
    /// </summary>
    /// <param name="info">External login info from Google's OAuth callback.</param>
    /// <returns>A <see cref="LoginResult"/> with the resolved user and claims on success.</returns>
    public async Task<LoginResult> HandleGoogleLoginAsync(ExternalLoginInfo info)
    {
        ArgumentNullException.ThrowIfNull(info);

        // 1. Check if this Google account is already linked to a local user
        var existingLinkedUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
        if (existingLinkedUser is not null)
        {
            return await BuildLoginResultAsync(existingLinkedUser);
        }

        // 2. Extract email from Google claims
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
        {
            return new LoginResult
            {
                Success = false,
                Errors = ["Google did not provide an email address."],
            };
        }

        // 3. Check if a local account with the same email already exists
        var existingEmailUser = await _userManager.FindByEmailAsync(email);
        if (existingEmailUser is not null)
        {
            // Link Google login to the existing account and confirm the email
            await _userManager.AddLoginAsync(existingEmailUser, info);
            if (!existingEmailUser.EmailConfirmed)
            {
                existingEmailUser.EmailConfirmed = true;
                await _userManager.UpdateAsync(existingEmailUser);
            }
            return await BuildLoginResultAsync(existingEmailUser);
        }

        // 4. No existing account — create a new one from Google profile
        var fullName = info.Principal.FindFirstValue(ClaimTypes.Name)
                       ?? email.Split('@')[0];

        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true,
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            return new LoginResult
            {
                Success = false,
                Errors = createResult.Errors.Select(e => e.Description).ToList(),
            };
        }

        await _userManager.AddLoginAsync(newUser, info);
        await _userManager.AddToRoleAsync(newUser, UserRole.Student.ToString());
        await _userManager.AddClaimsAsync(newUser,
        [
            new Claim(ClaimTypes.NameIdentifier, newUser.Id.ToString()),
            new Claim(ClaimTypes.Email, newUser.Email),
            new Claim(ClaimTypes.Name, newUser.FullName),
            new Claim(ClaimTypes.Role, UserRole.Student.ToString()),
        ]);

        return await BuildLoginResultAsync(newUser);
    }

    /// <summary>
    /// Signs out the currently authenticated user by clearing the authentication cookie.
    /// </summary>
    /// <returns>A task representing the asynchronous sign-out operation.</returns>
    public Task LogoutAsync() => Task.CompletedTask;

    /// <summary>
    /// Returns <c>true</c> when an account with the given email already exists in the store.
    /// </summary>
    /// <param name="email">The email address to look up.</param>
    public async Task<bool> EmailExistsAsync(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return await _userManager.FindByEmailAsync(email) is not null;
    }

    /// <summary>
    /// Creates a verified account using a pre-computed BCrypt hash.
    /// Assigns the Student role and the required identity claims.
    /// </summary>
    /// <param name="email">The verified email address.</param>
    /// <param name="fullName">The user's display name.</param>
    /// <param name="bcryptHash">BCrypt hash produced during OTP initiation — stored as-is.</param>
    /// <returns><c>null</c> on success, or an error message on failure.</returns>
    public async Task<string?> CreateVerifiedAccountAsync(string email, string fullName, string bcryptHash)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(fullName);
        ArgumentNullException.ThrowIfNull(bcryptHash);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            EmailConfirmed = true,
            IsActive = true,
            PasswordHash = bcryptHash, // Pre-computed BCrypt hash — CreateAsync(user) preserves it as-is
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            return createResult.Errors.FirstOrDefault()?.Description
                   ?? "Failed to create the account. Please try again.";
        }

        await _userManager.AddToRoleAsync(user, UserRole.Student.ToString());
        await _userManager.AddClaimsAsync(user,
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, UserRole.Student.ToString()),
        ]);

        return null;
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Loads stored claims for the user and returns a successful <see cref="LoginResult"/>.
    /// </summary>
    /// <param name="user">The resolved user entity.</param>
    private async Task<LoginResult> BuildLoginResultAsync(ApplicationUser user)
    {
        var claims = (await _userManager.GetClaimsAsync(user)).ToList();

        // Guarantee Name claim required by the antiforgery system
        if (!claims.Any(c => c.Type == ClaimTypes.Name))
            claims.Add(new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? "user"));

        if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));

        return new LoginResult
        {
            Success = true,
            User = user,
            Claims = claims,
            Errors = [],
        };
    }
}
