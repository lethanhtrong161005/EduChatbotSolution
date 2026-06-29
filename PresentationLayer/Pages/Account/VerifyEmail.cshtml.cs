using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Constants;
using Presentation.ViewModels;

namespace Presentation.Pages.Account;

/// <summary>
/// Handles email verification for newly registered accounts.
/// </summary>
[AllowAnonymous]
public class VerifyEmailModel(
    IAuthService authService,
    IEmailVerificationService emailVerificationService,
    UserManager<ApplicationUser> userManager,
    IResourceRealtimeNotifier notifier)
    : PageModel
{
    private readonly IAuthService _authService = authService;
    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IResourceRealtimeNotifier _notifier = notifier;

    /// <summary>
    /// Gets the email verification form data rendered by the page.
    /// </summary>
    public VerifyEmailVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Gets the URL preserved through the verification flow.
    /// </summary>
    public string ReturnUrl { get; private set; } = AuthenticationConstants.FallbackReturnUrl;

    /// <summary>
    /// Displays the email verification page with a pre-filled email address.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="returnUrl">The URL to preserve through verification.</param>
    /// <returns>The email verification page.</returns>
    public IActionResult OnGet(string email, string returnUrl = AuthenticationConstants.FallbackReturnUrl)
    {
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? AuthenticationConstants.FallbackReturnUrl
            : returnUrl;
        ViewModel = new VerifyEmailVm { Email = email };
        return Page();
    }

    /// <summary>
    /// Confirms the submitted verification code and activates the pending account.
    /// </summary>
    /// <param name="returnUrl">The URL to preserve after verification.</param>
    /// <returns>A redirect to login or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(
        [Bind(Prefix = nameof(ViewModel))] VerifyEmailVm viewModel,
        string returnUrl = AuthenticationConstants.FallbackReturnUrl)
    {
        ViewModel = viewModel;
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? AuthenticationConstants.FallbackReturnUrl
            : returnUrl;

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

        var existingUser = await _userManager.FindByEmailAsync(ViewModel.Email);
        if (existingUser is not null)
        {
            if (existingUser.EmailConfirmed)
            {
                TempData[AppConstants.TempDataSuccess] = "Email is already verified. Please log in.";
                return RedirectToPage("/Account/Login", new { returnUrl = ReturnUrl });
            }

            existingUser.EmailConfirmed = true;
            existingUser.UpdatedAt = DateTimeOffset.UtcNow;

            var updateResult = await _userManager.UpdateAsync(existingUser);
            if (!updateResult.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    updateResult.Errors.FirstOrDefault()?.Description ?? "Failed to update user confirmation status.");
                return Page();
            }

            await _emailVerificationService.CleanupAsync(ViewModel.Email);

            var upd = new ResourceUpdate
            {
                ResourceType = ResourceType.User,
                Action = ResourceAction.Enabled,
                ResourceId = existingUser.Id.ToString(),
                ResourceName = existingUser.FullName,
            };

            await _notifier.PushUpdateAsync(upd);

            TempData[AppConstants.TempDataSuccess] = AppConstants.RegistrationSuccess;
            return RedirectToPage("/Account/Login", new { returnUrl = ReturnUrl });
        }

        var pendingReg = await _emailVerificationService.GetPendingRegistrationAsync(ViewModel.Email);
        if (pendingReg is null)
        {
            ModelState.AddModelError(string.Empty, "Registration session has expired. Please register again.");
            return Page();
        }

        var createResult = await _authService.CreateVerifiedAccountAsync(
            ViewModel.Email,
            pendingReg.Value.FullName,
            pendingReg.Value.BcryptHash);

        if (createResult.Errors is not null)
        {
            ModelState.AddModelError(string.Empty, createResult.Errors);
            return Page();
        }

        await _emailVerificationService.CleanupAsync(ViewModel.Email);

        var user = createResult.User;

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Enabled,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update);

        TempData[AppConstants.TempDataSuccess] = AppConstants.RegistrationSuccess;
        return RedirectToPage("/Account/Login", new { returnUrl = ReturnUrl });
    }

    /// <summary>
    /// Re-sends a verification code and returns JSON so the client can restart the countdown timer.
    /// </summary>
    /// <param name="email">The email address that receives the verification code.</param>
    /// <returns>A JSON result containing resend status and cooldown information.</returns>
    public async Task<IActionResult> OnPostResendAsync([FromForm] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { success = false, error = "Email is required." });
        }

        var (success, error, remainingSeconds) = await _emailVerificationService.ResendCodeAsync(email);
        return new JsonResult(new { success, error, remainingSeconds });
    }
}
