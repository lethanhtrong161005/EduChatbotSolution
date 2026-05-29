using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BusinessLayer.DTOs;
using BusinessLayer.Services.Interfaces;
using System.Threading.Tasks;
using System;

namespace PresentationLayer.Controllers;

/// <summary>
/// Controller for handling user authentication and account operations.
/// Manages login, logout, and session-based authentication for the application.
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private const string UserSessionKey = "UserId";
    private const string RolesSessionKey = "UserRoles";
    private const string FullNameSessionKey = "FullName";

    /// <summary>
    /// Initializes a new instance of the AccountController.
    /// </summary>
    /// <param name="authService">The authentication service for user login operations.</param>
    public AccountController(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    /// <summary>
    /// Displays the login page.
    /// </summary>
    /// <returns>The login view.</returns>
    [HttpGet]
    public IActionResult Login()
    {
        // If user is already logged in, redirect to home
        if (HttpContext.Session.GetString(UserSessionKey) != null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    /// <summary>
    /// Processes user login request with email and password.
    /// Creates session upon successful authentication.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>Redirects to home on success or returns login view on failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    [HttpPost]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        if (request == null)
        {
            return BadRequest("Login request is required.");
        }

        // 1. Validate input
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            ModelState.AddModelError(string.Empty, "Email and password are required.");
            return View(request);
        }

        // 2. Authenticate user
        var response = await _authService.LoginAsync(request);

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View(request);
        }

        // 3. Create session
        HttpContext.Session.SetString(UserSessionKey, response.UserId.ToString());
        HttpContext.Session.SetString(FullNameSessionKey, response.FullName);
        
        // Store roles as comma-separated string
        var rolesString = string.Join(",", response.Roles);
        HttpContext.Session.SetString(RolesSessionKey, rolesString);

        // 4. Set session timeout (optional: configure in appsettings.json)
        if (request.RememberMe)
        {
            HttpContext.Session.SetString("RememberMe", "true");
        }

        return RedirectToAction("Index", "Home");
    }

    /// <summary>
    /// Displays the registration page for new user account creation.
    /// Redirects to home if the user is already logged in.
    /// </summary>
    /// <returns>The register view with an empty registration form.</returns>
    [HttpGet]
    [Route("register-account")]
    public IActionResult Register()
    {
        // If user is already logged in, redirect to home
        if (HttpContext.Session.GetString(UserSessionKey) != null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    /// <summary>
    /// Processes a new user registration request.
    /// Validates the model, checks email uniqueness, and creates the account.
    /// </summary>
    /// <param name="request">The registration request containing user details and password.</param>
    /// <returns>Redirects to Login with a success message on success, or returns Register view with errors on failure.</returns>
    [HttpPost]
    [Route("register-account")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        // 1. If user is already logged in, redirect to home
        if (HttpContext.Session.GetString(UserSessionKey) != null)
        {
            return RedirectToAction("Index", "Home");
        }

        // 2. Validate model annotations
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        // 3. Attempt registration via service
        var response = await _authService.RegisterAsync(request);

        if (!response.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, response.Message);
            return View(request);
        }

        // 4. Set success message for Login page and redirect
        TempData["SuccessMessage"] = "Account created successfully! Please log in.";
        return RedirectToAction("Login");
    }

    /// <summary>
    /// Clears user session and logs out the current user.
    /// </summary>
    /// <returns>Redirects to login page after logout.</returns>
    [HttpPost]
    public IActionResult Logout()
    {
        // Clear session
        HttpContext.Session.Clear();

        return RedirectToAction("Login");
    }
}
