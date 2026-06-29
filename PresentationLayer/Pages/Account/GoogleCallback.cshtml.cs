using Domain.Constants;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Account;

/// <summary>
/// Handles the Google OAuth callback and signs in the linked or newly created user.
/// </summary>
[AllowAnonymous]
public class GoogleCallbackModel(
    IAuthService authService,
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    private readonly IAuthService _authService = authService;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    /// <summary>
    /// Processes the OAuth callback from Google and signs in the user.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful authentication.</param>
    /// <returns>A redirect to the requested URL or the login page.</returns>
    public async Task<IActionResult> OnGetAsync(string returnUrl = "/home")
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
