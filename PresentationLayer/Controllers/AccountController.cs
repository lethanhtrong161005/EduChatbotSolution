using Domain.Common;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Defaults;
using Presentation.Models;

namespace Presentation.Controllers;

/// <summary>
/// Handles user authentication operations including login, registration, and logout.
/// Uses <see cref="IAuthService"/> for BCrypt credential verification and cookie sign-in.
/// </summary>
public class AccountController : Controller
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountController"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    public AccountController(IAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
    }

    // ── LOGIN ────────────────────────────────────────────────

    /// <summary>
    /// Displays the login page. Redirects authenticated users to the home page.
    /// </summary>
    /// <param name="returnUrl">Optional URL to redirect to after successful login.</param>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(returnUrl ?? AuthenticationDefaults.FallbackReturnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Processes the login form. Authenticates via BCrypt and signs in with a cookie.
    /// </summary>
    /// <param name="model">Login credentials.</param>
    /// <param name="returnUrl">Optional return URL after login.</param>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequestViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var success = await _authService.LoginAsync(model.Email, model.Password, model.RememberMe);

        if (success)
        {
            return Redirect(returnUrl ?? AuthenticationDefaults.FallbackReturnUrl);
        }

        ModelState.AddModelError(string.Empty, AppConstants.InvalidCredentials);
        return View(model);
    }

    // ── REGISTER ─────────────────────────────────────────────

    /// <summary>
    /// Displays the registration page.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return Redirect(AuthenticationDefaults.FallbackReturnUrl);
        }

        return View();
    }

    /// <summary>
    /// Processes the registration form. Creates a new account with BCrypt-hashed password.
    /// Redirects to login with a success message on completion.
    /// </summary>
    /// <param name="model">Registration data.</param>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var error = await _authService.RegisterAsync(model.Email, model.FullName, model.Password);

        if (error is null)
        {
            TempData[AppConstants.TempDataSuccess] = AppConstants.RegistrationSuccess;
            return RedirectToAction(nameof(Login));
        }

        ModelState.AddModelError(string.Empty, error);
        return View(model);
    }

    // ── LOGOUT ───────────────────────────────────────────────

    /// <summary>
    /// Signs out the currently authenticated user and redirects to the login page.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync();
        return RedirectToAction(nameof(Login));
    }
}
