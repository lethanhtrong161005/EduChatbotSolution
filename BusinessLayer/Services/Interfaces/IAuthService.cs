using BusinessLayer.DTOs;
using System.Threading.Tasks;

namespace BusinessLayer.Services.Interfaces;

/// <summary>
/// Interface defining authentication and authorization service operations.
/// Handles user login, registration, and password management.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>An authentication response indicating success or failure.</returns>
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);

    /// <summary>
    /// Registers a new user account with the Student role.
    /// Validates uniqueness of email and hashes the password before persisting.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <returns>An authentication response indicating success or failure with details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);

    /// <summary>
    /// Validates a user password against a stored hash.
    /// </summary>
    /// <param name="password">The plaintext password to validate.</param>
    /// <param name="hash">The stored password hash.</param>
    /// <returns>True if password matches hash; otherwise false.</returns>
    bool VerifyPassword(string password, string hash);

    /// <summary>
    /// Generates a secure hash for a plaintext password.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>The generated password hash.</returns>
    string HashPassword(string password);
}
