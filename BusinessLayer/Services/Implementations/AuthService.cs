using DataAccess.Data;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Business.Services.Implementations;

/// <summary>
/// Provides custom authentication and registration using BCrypt password hashing
/// and ASP.NET Core cookie authentication. Uses the custom database schema
/// (not ASP.NET Identity) aligned with <c>database-script.sql</c>.
/// </summary>
public class AuthService : IAuthService
{
    private readonly EduChatbotDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="context">The EF Core database context.</param>
    /// <param name="httpContextAccessor">The HTTP context accessor for signing in/out.</param>
    public AuthService(EduChatbotDbContext context, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// Authenticates a user by email and password using BCrypt verification.
    /// Signs in via a cookie on success.
    /// </summary>
    /// <param name="request">Login credentials including email, password and remember-me flag.</param>
    /// <returns><c>true</c> if authentication succeeded; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    public async Task<bool> LoginAsync(string email, string password, bool rememberMe)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        // 1. Find active user by email
        var emailClaim = new Claim(ClaimTypes.Email, email);
        var user = (await _userManager.GetUsersForClaimAsync(emailClaim)).First();
        if (user is null)
            return false;

        // 2. Verify password with BCrypt
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return false;
        }

        // 3. Load role for claims
        var userRole = (await _userManager.GetRolesAsync(user)).First();

        // 4. Build claims identity and sign in
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
        };

        if (userRole is not null)
        {
            claims.Add(new Claim(ClaimTypes.Role, userRole));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProps = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe
                ? DateTimeOffset.UtcNow.AddDays(30)
                : DateTimeOffset.UtcNow.AddHours(8)
        };

        await _httpContextAccessor.HttpContext!.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProps);

        return true;
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
        var exists = await _context.Users.AnyAsync(u => u.Email == email);
        if (exists)
        {
            return "An account with this email address already exists.";
        }

        // 2. Resolve the Student role
        var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == UserRole.Student.ToString());
        if (studentRole is null)
        {
            return "System configuration error: default role not found. Please contact the administrator.";
        }

        // 3. Hash password and create the user entity
        var user = new ApplicationUser
        {
            Email = email,
            FullName = fullName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        };

        // 4. Assign Student role
        await _userManager.AddToRoleAsync(user, UserRole.Student.ToString());

        return null; // null = success
    }

    /// <summary>
    /// Signs out the currently authenticated user by clearing the authentication cookie.
    /// </summary>
    /// <returns>A task representing the asynchronous sign-out operation.</returns>
    public async Task LogoutAsync()
    {
        await _httpContextAccessor.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
