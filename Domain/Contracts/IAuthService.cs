
using Domain.Entities;
using Microsoft.AspNetCore.Http.Authentication;
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
    /// Signs the user in via an authentication cookie on success.
    /// </summary>
    /// <param name="request">The login request containing email, password, and remember-me flag.</param>
    /// <returns><c>true</c> if authentication succeeded; otherwise <c>false</c>.</returns>
    Task<LoginResult> LoginAsync(string email, string password);

    /// <summary>
    /// Creates a new user account with a BCrypt-hashed password
    /// and assigns the default Student role.
    /// </summary>
    /// <param name="request">The registration request containing email, full name, and password.</param>
    /// <returns>
    /// <c>null</c> on success, or an error message string describing the failure.
    /// </returns>
    Task<string?> RegisterAsync(string email, string fullName, string password);

    /// <summary>
    /// Signs out the currently authenticated user and clears the authentication cookie.
    /// </summary>
    Task LogoutAsync();
}

public readonly struct LoginResult
{
    public bool Success { get; init; }
    public ApplicationUser User { get; init; }
    public IList<Claim> Claims { get; init; }
    public IList<string> Errors { get; init; }
}
