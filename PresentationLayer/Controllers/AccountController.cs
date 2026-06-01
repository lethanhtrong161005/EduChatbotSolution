using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Presentation.Defaults;
using Presentation.Models;

namespace Presentation.Controllers;

/// <summary>
/// Handles user authentication operations including login, registration, and logout.
/// Uses <see cref="IAuthService"/> for BCrypt credential verification and cookie sign-in.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AccountController"/> class.
/// </remarks>
/// <param name="authService">The authentication service.</param>
[Authorize]
public class AccountController(IAuthService authService, SignInManager<ApplicationUser> signInManager) : Controller
{
    private readonly IAuthService _authService = authService;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    // ── LOGIN ────────────────────────────────────────────────

    /// <summary>
    /// Displays the login page. Redirects authenticated users to the home page.
    /// </summary>
    /// <param name="returnUrl">Optional URL to redirect to after successful login.</param>
    [HttpGet(AuthenticationSettings.LoginPath)]
    [AllowAnonymous]
    public IActionResult Login(string returnUrl = AuthenticationSettings.FallbackReturnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(returnUrl ?? AuthenticationSettings.FallbackReturnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Processes the login form. Authenticates via BCrypt and signs in with a cookie.
    /// </summary>
    /// <param name="model">Login credentials.</param>
    /// <param name="returnUrl">Optional return URL after login.</param>
    [HttpPost(AuthenticationSettings.LoginPath)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginRequestVm model,
        string returnUrl = AuthenticationSettings.FallbackReturnUrl)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var loginResult = await _authService.LoginAsync(model.Email, model.Password);

        if (loginResult.Success)
        {
            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                             ? DateTimeOffset.UtcNow.AddDays(30)
                             : DateTimeOffset.UtcNow.AddHours(8)
            };

            await _signInManager.SignInWithClaimsAsync(loginResult.User, authProps, loginResult.Claims);
            return LocalRedirect(returnUrl ?? AuthenticationSettings.FallbackReturnUrl);
        }

        ModelState.AddModelError(string.Empty, AppConstants.InvalidCredentials);
        return View(model);
    }

    // ── REGISTER ─────────────────────────────────────────────

    /// <summary>
    /// Displays the registration page.
    /// </summary>
    [HttpGet(AuthenticationSettings.RegistrationPath)]
    [AllowAnonymous]
    public IActionResult Register(string returnUrl = AuthenticationSettings.FallbackReturnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(AuthenticationSettings.FallbackReturnUrl);
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    /// <summary>
    /// Processes the registration form. Creates a new account with BCrypt-hashed password.
    /// Redirects to login with a success message on completion.
    /// </summary>
    /// <param name="model">Registration data.</param>
    [HttpPost(AuthenticationSettings.RegistrationPath)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterRequestVm model,
        string returnUrl = AuthenticationSettings.FallbackReturnUrl)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var error = await _authService.RegisterAsync(model.Email, model.FullName, model.Password);

        if (error is null)
        {
            TempData[AppConstants.TempDataSuccess] = AppConstants.RegistrationSuccess;
            return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
        }

        ModelState.AddModelError(string.Empty, error);
        return View(model);
    }

    // ── LOGOUT ───────────────────────────────────────────────

    /// <summary>
    /// Signs out the currently authenticated user and redirects to the login page.
    /// </summary>
    [HttpPost(AuthenticationSettings.LogoutPath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
}
