using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Domain.Contracts;

/// <summary>
/// Defines the contract for user authentication and registration operations.
/// Uses custom BCrypt + cookie authentication aligned with the project database schema.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user using email and BCrypt-verified password.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The plain-text password to verify.</param>
    /// <returns>A <see cref="LoginResult"/> containing the user and claims on success.</returns>
    Task<LoginResult> LoginAsync(string email, string password);

    /// <summary>
    /// Creates a new user account with a BCrypt-hashed password
    /// and assigns the default Student role.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="fullName">The user's display name.</param>
    /// <param name="password">The plain-text password to hash and store.</param>
    /// <returns>
    /// <c>null</c> on success, or an error message string describing the failure.
    /// </returns>
    Task<string?> RegisterAsync(string email, string fullName, string password);

    /// <summary>
    /// Resolves a Google OAuth login: finds an existing linked account, links Google to an
    /// existing email account, or creates a brand-new account — whichever applies.
    /// Email verification is automatically set to <c>true</c> because Google guarantees it.
    /// </summary>
    /// <param name="info">The external login info returned by Google after OAuth consent.</param>
    /// <returns>A <see cref="LoginResult"/> containing the resolved user and claims on success.</returns>
    Task<LoginResult> HandleGoogleLoginAsync(ExternalLoginInfo info);

    /// <summary>
    /// Signs out the currently authenticated user and clears the authentication cookie.
    /// </summary>
    Task LogoutAsync();

    /// <summary>
    /// Checks whether an account with the given email already exists in the store.
    /// Used before initiating the email verification flow to catch duplicates early.
    /// </summary>
    /// <param name="email">The email address to look up.</param>
    /// <returns><c>true</c> if an account exists; otherwise <c>false</c>.</returns>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Creates a verified user account using a pre-computed BCrypt password hash.
    /// Called after email OTP verification succeeds so the password is never re-hashed.
    /// Assigns the default Student role and required claims.
    /// </summary>
    /// <param name="email">The verified email address.</param>
    /// <param name="fullName">The user's display name.</param>
    /// <param name="bcryptHash">A BCrypt hash of the user's password, produced during OTP initiation.</param>
    /// <returns><c>null</c> on success, or an error message string on failure.</returns>
    Task<string?> CreateVerifiedAccountAsync(string email, string fullName, string bcryptHash);
}

/// <summary>
/// Represents the result of an authentication attempt.
/// </summary>
public readonly struct LoginResult
{
    /// <summary>Gets whether the authentication succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Gets the authenticated user entity.</summary>
    public ApplicationUser User { get; init; }

    /// <summary>Gets the claims to attach to the session cookie.</summary>
    public IList<Claim> Claims { get; init; }

    /// <summary>Gets error messages when authentication failed.</summary>
    public IList<string> Errors { get; init; }
}
