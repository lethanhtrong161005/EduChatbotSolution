using Domain.Constants;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Constants;
using Presentation.ViewModels;

namespace Presentation.Pages.Account;

/// <summary>
/// Handles the mandatory password change for admin-created accounts.
/// Users are redirected here from the login page when their
/// <see cref="ApplicationUser.MustChangePassword"/> flag is <c>true</c>.
/// </summary>
[AllowAnonymous]
public class ForceChangePasswordModel(
    UserManager<ApplicationUser> userManager) : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    /// <summary>
    /// Gets the form data rendered by the page.
    /// </summary>
    public ForceChangePasswordVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Displays the force-change-password page.
    /// The email is passed securely via TempData from the login page.
    /// </summary>
    public IActionResult OnGet()
    {
        var email = TempData[AppConstants.TempDataForceChangeEmail] as string;

        if (string.IsNullOrWhiteSpace(email))
        {
            // No email in TempData — user likely refreshed or navigated directly.
            // Redirect to login to start over.
            return RedirectToPage("/Account/Login");
        }

        ViewModel = new ForceChangePasswordVm { Email = email };

        // Keep email in TempData for POST (TempData is consumed on read)
        TempData[AppConstants.TempDataForceChangeEmail] = email;

        return Page();
    }

    /// <summary>
    /// Verifies the current password, saves the new password, and clears the
    /// <see cref="ApplicationUser.MustChangePassword"/> flag.
    /// </summary>
    public async Task<IActionResult> OnPostAsync(
        [Bind(Prefix = nameof(ViewModel))] ForceChangePasswordVm viewModel)
    {
        ViewModel = viewModel;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // 1. Find user
        var user = await _userManager.FindByEmailAsync(ViewModel.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Account not found.");
            return Page();
        }

        // 2. Verify current (auto-generated) password
        if (!BCrypt.Net.BCrypt.Verify(ViewModel.CurrentPassword, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Current password is incorrect.");
            return Page();
        }

        // 3. Ensure new password differs from current
        if (ViewModel.CurrentPassword == ViewModel.NewPassword)
        {
            ModelState.AddModelError(string.Empty,
                "New password must be different from your current password.");
            return Page();
        }

        // 4. Hash and save new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(ViewModel.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty,
                result.Errors.FirstOrDefault()?.Description ?? "Failed to update password.");
            return Page();
        }

        TempData[AppConstants.TempDataSuccess] =
            "Password changed successfully! Please sign in with your new password.";

        return RedirectToPage("/Account/Login");
    }
}
