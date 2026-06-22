using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.ViewModels;

namespace Presentation.Pages.Account;

/// <summary>
/// Handles password reset verification and password replacement.
/// </summary>
[AllowAnonymous]
public class ResetPasswordModel(
    IAuthService authService,
    IEmailVerificationService emailVerificationService,
    UserManager<ApplicationUser> userManager) : PageModel
{
    private readonly IAuthService _authService = authService;
    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    /// <summary>
    /// Gets the password reset form data rendered by the page.
    /// </summary>
    public ResetPasswordVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Displays the reset page for the requested account email.
    /// </summary>
    /// <param name="email">The account email address that requested password reset.</param>
    /// <returns>The reset password page.</returns>
    public IActionResult OnGet(string email)
    {
        ViewModel = new ResetPasswordVm { Email = email };
        return Page();
    }

    /// <summary>
    /// Verifies the submitted reset code and stores the new password hash.
    /// </summary>
    /// <returns>A redirect to login or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync([Bind(Prefix = nameof(ViewModel))] ResetPasswordVm viewModel)
    {
        ViewModel = viewModel;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var (codeOk, codeError) = await _emailVerificationService.VerifyCodeAsync(ViewModel.Email, ViewModel.Code);
        if (!codeOk)
        {
            ModelState.AddModelError(string.Empty, codeError ?? "Verification failed.");
            return Page();
        }

        var pending = await _emailVerificationService.GetPendingPasswordResetAsync(ViewModel.Email);
        if (pending is null)
        {
            ModelState.AddModelError(string.Empty, "Password reset session has expired. Please request a new code.");
            return Page();
        }

        var user = await _userManager.FindByEmailAsync(ViewModel.Email);
        if (user is null || user.Id != pending.Value.UserId)
        {
            ModelState.AddModelError(string.Empty, "Password reset session is no longer valid. Please request a new code.");
            return Page();
        }

        var resetError = await _authService.ResetPasswordAsync(ViewModel.Email, ViewModel.Password);
        if (resetError is not null)
        {
            ModelState.AddModelError(string.Empty, resetError);
            return Page();
        }

        await _emailVerificationService.CleanupPasswordResetAsync(ViewModel.Email);

        TempData[AppConstants.TempDataSuccess] = "Password reset successfully. Please sign in with your new password.";
        return RedirectToPage("/Account/Login");
    }
}
