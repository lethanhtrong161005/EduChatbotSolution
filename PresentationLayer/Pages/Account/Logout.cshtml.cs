using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Domain.Entities;

namespace Presentation.Pages.Account;

/// <summary>
/// Handles user sign-out from Razor Pages forms.
/// </summary>
[AllowAnonymous]
public class LogoutModel(SignInManager<ApplicationUser> signInManager) : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    /// <summary>
    /// Redirects direct GET requests back to the login page.
    /// </summary>
    /// <returns>A redirect to the login page.</returns>
    public IActionResult OnGet()
    {
        return RedirectToPage("/Account/Login");
    }

    /// <summary>
    /// Signs out the current user and redirects to the login page.
    /// </summary>
    /// <returns>A redirect to the login page.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Account/Login");
    }
}
