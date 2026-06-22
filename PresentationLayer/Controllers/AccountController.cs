using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Presentation.Constants;

namespace Presentation.Controllers;

/// <summary>
/// Exposes authentication endpoints that return JSON or external OAuth redirects.
/// Razor Pages handle all page-rendering account workflows.
/// </summary>
[AllowAnonymous]
public class AccountController(
    IAuthService authService,
    IEmailVerificationService emailVerificationService,
    SignInManager<ApplicationUser> signInManager) : Controller
{
    private readonly IAuthService _authService = authService;
    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    /// <summary>
    /// Re-sends a verification code and returns JSON so the client can restart the countdown timer.
    /// </summary>
    /// <param name="email">The email address that receives the verification code.</param>
    /// <returns>A JSON result containing resend status and cooldown information.</returns>
    [HttpPost(AuthenticationConstants.ResendCodePath)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendCode([FromForm] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { success = false, error = "Email is required." });
        }

        var (success, error, remainingSeconds) = await _emailVerificationService.ResendCodeAsync(email);
        return Json(new { success, error, remainingSeconds });
    }

    /// <summary>
    /// Starts the Google OAuth flow by issuing a challenge redirect to Google.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful authentication.</param>
    /// <returns>A challenge result for the Google authentication handler.</returns>
    [HttpGet(AuthenticationConstants.GoogleLoginPath)]
    public IActionResult GoogleLogin(string returnUrl = AuthenticationConstants.FallbackReturnUrl)
    {
        var callbackUrl = Url.Action(nameof(GoogleCallback), "Account", new { returnUrl }, Request.Scheme)
                         ?? AuthenticationConstants.FallbackReturnUrl;
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", callbackUrl);
        return Challenge(properties, "Google");
    }

    /// <summary>
    /// Handles the Google OAuth callback and signs in the linked or newly created user.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful authentication.</param>
    /// <returns>A redirect to the requested URL or the login page.</returns>
    [HttpGet(AuthenticationConstants.GoogleCallbackAction)]
    public async Task<IActionResult> GoogleCallback(string returnUrl = AuthenticationConstants.FallbackReturnUrl)
    {
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            TempData[AppConstants.TempDataError] = "Google sign-in failed. Please try again.";
            return RedirectToPage("/Account/Login");
        }

        var result = await _authService.HandleGoogleLoginAsync(info);
        if (result.Success)
        {
            var authProps = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30),
            };

            await _signInManager.SignInWithClaimsAsync(result.User, authProps, result.Claims);
            return LocalRedirect(returnUrl);
        }

        TempData[AppConstants.TempDataError] = string.Join(" ", result.Errors);
        return RedirectToPage("/Account/Login");
    }
}
