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
/// Handles user authentication operations including login, registration,
/// email verification, and logout. Uses <see cref="IAuthService"/> for credential
/// management and <see cref="IEmailVerificationService"/> for OTP flow.
/// </summary>
[Authorize]
public class AccountController(
    IAuthService authService,
    IEmailVerificationService emailVerificationService,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    private readonly IAuthService _authService = authService;
    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;
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
    /// Validates the registration form, checks for duplicate email, then sends an OTP
    /// and redirects to the email verification page. Account creation is deferred until
    /// the OTP is verified.
    /// </summary>
    /// <param name="model">Registration data.</param>
    /// <param name="returnUrl">Optional return URL after eventual login.</param>
    [HttpPost(AuthenticationSettings.RegistrationPath)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterRequestVm model,
        string returnUrl = AuthenticationSettings.FallbackReturnUrl)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        if (await _authService.EmailExistsAsync(model.Email))
        {
            ModelState.AddModelError(string.Empty, "An account with this email address already exists.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var (success, error) = await _emailVerificationService.InitiateVerificationAsync(
            model.Email, model.FullName, model.Password);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to send verification code.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        return RedirectToAction(nameof(VerifyEmail), new { email = model.Email, returnUrl });
    }

    // ── EMAIL VERIFICATION ────────────────────────────────────

    /// <summary>
    /// Displays the email verification page with a pre-filled email address.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="returnUrl">Optional return URL after successful account creation.</param>
    [HttpGet(AuthenticationSettings.VerifyEmailPath)]
    [AllowAnonymous]
    public IActionResult VerifyEmail(string email, string returnUrl = AuthenticationSettings.FallbackReturnUrl)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new VerifyEmailVm { Email = email });
    }

    /// <summary>
    /// Verifies the submitted OTP, creates the user account from pending Redis data,
    /// and redirects to the login page with a success message.
    /// </summary>
    /// <param name="model">The email address and 6-digit code from the verification form.</param>
    /// <param name="returnUrl">Optional return URL after login.</param>
    [HttpPost(AuthenticationSettings.VerifyEmailPath)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(
        VerifyEmailVm model,
        string returnUrl = AuthenticationSettings.FallbackReturnUrl)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var (codeOk, codeError) = await _emailVerificationService.VerifyCodeAsync(model.Email, model.Code);
        if (!codeOk)
        {
            ModelState.AddModelError(string.Empty, codeError ?? "Verification failed.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var pending = await _emailVerificationService.GetPendingRegistrationAsync(model.Email);
        if (pending is null)
        {
            ModelState.AddModelError(string.Empty,
                "Registration session has expired. Please register again.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        var createError = await _authService.CreateVerifiedAccountAsync(
            model.Email, pending.Value.FullName, pending.Value.BcryptHash);

        if (createError is not null)
        {
            ModelState.AddModelError(string.Empty, createError);
            ViewData["ReturnUrl"] = returnUrl;
            return View(model);
        }

        await _emailVerificationService.CleanupAsync(model.Email);

        TempData[AppConstants.TempDataSuccess] = AppConstants.RegistrationSuccess;
        return RedirectToAction(nameof(Login), new { ReturnUrl = returnUrl });
    }

    /// <summary>
    /// Re-sends a new OTP to the email. Returns JSON so the client can restart the countdown timer.
    /// </summary>
    /// <param name="email">The email address to resend the verification code to.</param>
    [HttpPost(AuthenticationSettings.ResendCodePath)]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendCode([FromForm] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { success = false, error = "Email is required." });
        }

        var (success, error, remainingSeconds) =
            await _emailVerificationService.ResendCodeAsync(email);

        return Json(new { success, error, remainingSeconds });
    }

    // ── GOOGLE OAUTH ─────────────────────────────────────────────

    /// <summary>
    /// Initiates the Google OAuth flow by issuing a challenge redirect to Google.
    /// </summary>
    /// <param name="returnUrl">URL to redirect to after successful authentication.</param>
    [HttpGet(AuthenticationSettings.GoogleLoginPath)]
    [AllowAnonymous]
    public IActionResult GoogleLogin(string returnUrl = AuthenticationSettings.FallbackReturnUrl)
    {
        var callbackUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl }, Request.Scheme)
                         ?? AuthenticationSettings.FallbackReturnUrl;
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", callbackUrl);
        return Challenge(properties, "Google");
    }

    /// <summary>
    /// Handles the Google OAuth callback. Finds, links, or creates a user account,
    /// then signs in and redirects to <paramref name="returnUrl"/>.
    /// </summary>
    /// <param name="returnUrl">URL to redirect to after successful authentication.</param>
    [HttpGet(AuthenticationSettings.GoogleCallbackAction)]
    [AllowAnonymous]
    public async Task<IActionResult> GoogleCallback(string returnUrl = AuthenticationSettings.FallbackReturnUrl)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ModelState.AddModelError(string.Empty, "Google sign-in failed. Please try again.");
            return View(nameof(Login));
        }

        var result = await _authService.HandleGoogleLoginAsync(info);
        if (result.Success)
        {
            var authProps = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
            };

            await _signInManager.SignInWithClaimsAsync(result.User, authProps, result.Claims);
            return LocalRedirect(returnUrl);
        }

        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error);

        return View(nameof(Login));
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
