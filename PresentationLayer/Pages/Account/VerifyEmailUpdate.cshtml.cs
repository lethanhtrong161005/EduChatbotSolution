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
/// Handles verification for administrator-initiated email address changes.
/// </summary>
[AllowAnonymous]
public class VerifyEmailUpdateModel(
    IEmailVerificationService emailVerificationService,
    UserManager<ApplicationUser> userManager,
    IResourceRealtimeNotifier notifier)
    : PageModel
{
    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IResourceRealtimeNotifier _notifier = notifier;

    /// <summary>
    /// Gets the email verification form data rendered by the page.
    /// </summary>
    public VerifyEmailVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Gets the URL preserved by the shared verification view.
    /// </summary>
    public string ReturnUrl { get; private set; } = AuthenticationConstants.FallbackReturnUrl;

    /// <summary>
    /// Displays the email update verification page with a pre-filled email address.
    /// </summary>
    /// <param name="email">The new email address to verify.</param>
    /// <returns>The email verification page.</returns>
    public IActionResult OnGet(string email)
    {
        ViewModel = new VerifyEmailVm { Email = email };
        return Page();
    }

    /// <summary>
    /// Verifies the submitted code and applies the pending email address update.
    /// </summary>
    /// <returns>A redirect to login or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync([Bind(Prefix = nameof(ViewModel))] VerifyEmailVm viewModel)
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

        var pending = await _emailVerificationService.GetPendingEmailUpdateAsync(ViewModel.Email);
        if (pending is null)
        {
            ModelState.AddModelError(
                string.Empty,
                "Email update session has expired. Please ask the administrator to retry.");
            return Page();
        }

        var user = await _userManager.FindByIdAsync(pending.Value.UserId.ToString());
        if (user is not null)
        {
            user.Email = ViewModel.Email;
            user.UserName = ViewModel.Email;
            user.NormalizedEmail = ViewModel.Email.ToUpperInvariant();
            user.NormalizedUserName = ViewModel.Email.ToUpperInvariant();
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.User,
                Action = ResourceAction.Enabled,
                ResourceId = user!.Id.ToString(),
                ResourceName = user.FullName,
            };

            await _notifier.PushUpdateAsync(update);
        }

        await _emailVerificationService.CleanupEmailUpdateAsync(ViewModel.Email);

        TempData[AppConstants.TempDataSuccess] =
            "Email verified successfully! Please log in with your new email address.";

        return RedirectToPage("/Account/Login");
    }
}
