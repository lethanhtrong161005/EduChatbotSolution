using BusinessLayer.DTOs;
using BusinessLayer.Services.Interfaces;
using DataAccessLayer.Data;
using DataAccessLayer.Entities;
using DataAccessLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BusinessLayer.Services.Implementations;

/// <summary>
/// Service implementation for authentication and authorization operations.
/// Handles user login, password validation, and authentication state management.
/// </summary>
public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of the AuthService.
    /// </summary>
    /// <param name="userRepository">The user repository for data access.</param>
    /// <param name="context">The database context for role lookups during registration.</param>
    public AuthService(IUserRepository userRepository, ApplicationDbContext context)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// Validates user credentials and returns authentication status with user information.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>An authentication response indicating success or failure with user details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Login request cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Email and password are required."
            };
        }

        // 1. Retrieve user by email
        var user = await _userRepository.GetUserByEmailAsync(request.Email);

        if (user == null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password."
            };
        }

        // 2. Check if account is active
        if (!user.IsActive)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Your account is inactive. Please contact the administrator."
            };
        }

        // 3. Verify password
        if (!VerifyPassword(request.Password, user.PasswordHash))
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Invalid email or password."
            };
        }

        // 4. Build response with user information and roles
        var roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>();

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Login successful.",
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Roles = roles
        };
    }

    /// <summary>
    /// Registers a new user account and assigns the default Student role.
    /// Validates email uniqueness, hashes the password, and persists the user.
    /// </summary>
    /// <param name="request">The registration request containing full name, email, and password.</param>
    /// <returns>An authentication response indicating success or failure with a descriptive message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request), "Registration request cannot be null.");
        }

        // 1. Check email uniqueness
        var emailExists = await _userRepository.UserExistsByEmailAsync(request.Email);
        if (emailExists)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "An account with this email address already exists."
            };
        }

        // 2. Lookup the default Student role
        var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student");
        if (studentRole == null)
        {
            return new AuthResponseDto
            {
                IsSuccess = false,
                Message = "Registration is temporarily unavailable. Please try again later."
            };
        }

        // 3. Build new user entity with hashed password
        var newUser = new User
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            PasswordHash = HashPassword(request.Password),
            IsEmailVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UserRoles = new List<UserRole>
            {
                new UserRole { RoleId = studentRole.Id }
            }
        };

        // 4. Persist the user
        var createdUser = await _userRepository.CreateUserAsync(newUser);

        return new AuthResponseDto
        {
            IsSuccess = true,
            Message = "Registration successful. Please log in.",
            UserId = createdUser.Id,
            FullName = createdUser.FullName,
            Email = createdUser.Email,
            Roles = new List<string> { studentRole.Name }
        };
    }

    /// <summary>
    /// Validates a user password against a stored hash using BCrypt algorithm.
    /// </summary>
    /// <param name="password">The plaintext password to validate.</param>
    /// <param name="hash">The stored password hash.</param>
    /// <returns>True if password matches hash; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when password or hash is null or whitespace.</exception>
    public bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentNullException(nameof(password), "Password cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentNullException(nameof(hash), "Password hash cannot be null or whitespace.");
        }

        // Use BCrypt to verify the password
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    /// <summary>
    /// Generates a secure hash for a plaintext password using BCrypt algorithm.
    /// </summary>
    /// <param name="password">The plaintext password to hash.</param>
    /// <returns>The generated BCrypt password hash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when password is null or whitespace.</exception>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentNullException(nameof(password), "Password cannot be null or whitespace.");
        }

        // Use BCrypt with salt rounds of 12 for strong hashing
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }
}
