using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Account;

/// <summary>
/// Initiates the Google OAuth authentication flow.
/// </summary>
[AllowAnonymous]
public class GoogleLoginModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    /// <summary>
    /// Starts the Google OAuth flow by issuing a challenge redirect to Google.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful authentication.</param>
    /// <returns>A challenge result for the Google authentication handler.</returns>
    public IActionResult OnGet(string returnUrl = "/home")
    {
        var callbackUrl = Url.Page("/Account/GoogleCallback", null, new { returnUrl }, Request.Scheme)
                         ?? "/home";
        var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", callbackUrl);
        return Challenge(properties, "Google");
    }
}
