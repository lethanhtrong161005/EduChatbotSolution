using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Constants;
using Presentation.ViewModels;

namespace Presentation.Pages.Account;

/// <summary>
/// Handles password reset requests for anonymous users.
/// </summary>
[AllowAnonymous]
public class ForgotPasswordModel(
    IEmailVerificationService emailVerificationService,
    UserManager<ApplicationUser> userManager) : PageModel
{
    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    /// <summary>
    /// Gets the forgot-password form data rendered by the page.
    /// </summary>
    public ForgotPasswordVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Displays the forgot-password page for anonymous users.
    /// </summary>
    /// <returns>The forgot-password page or a redirect for authenticated users.</returns>
    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(AuthenticationConstants.FallbackReturnUrl);
        }

        return Page();
    }

    /// <summary>
    /// Sends a password reset code when the submitted email belongs to an active account.
    /// </summary>
    /// <returns>A redirect to the reset page or the current page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync([Bind(Prefix = nameof(ViewModel))] ForgotPasswordVm viewModel)
    {
        ViewModel = viewModel;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(ViewModel.Email);
        if (user is not null && user.IsActive && !user.DeletedAt.HasValue)
        {
            var (success, error) = await _emailVerificationService.InitiatePasswordResetAsync(
                user.Email ?? ViewModel.Email,
                user.FullName,
                user.Id);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, error ?? "Failed to send password-reset code.");
                return Page();
            }
        }

        TempData[AppConstants.TempDataSuccess] =
            "If an account exists for that email, a password-reset code has been sent.";

        return RedirectToPage("/Account/ResetPassword", new { email = ViewModel.Email });
    }
}
