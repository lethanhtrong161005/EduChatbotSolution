using Domain.Constants;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.Antiforgery;
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
    IResourceRealtimeNotifier notifier,
    IAntiforgery antiforgery)
    : PageModel
{
  private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;
  private readonly UserManager<ApplicationUser> _userManager = userManager;
  private readonly IResourceRealtimeNotifier _notifier = notifier;
  private readonly IAntiforgery _antiforgery = antiforgery;

  /// <summary>
  /// Gets the email verification form data rendered by the page.
  /// </summary>
  public VerifyEmailVm ViewModel { get; private set; } = new();

  /// <summary>
  /// Gets the URL preserved by the shared verification view.
  /// </summary>
  public string ReturnUrl { get; private set; } = AuthenticationConstants.FallbackReturnUrl;

  /// <summary>
  /// Gets the actual remaining seconds of the OTP key in Redis.
  /// 0 means the code is already expired — the Resend button will be enabled immediately.
  /// </summary>
  public int OtpRemainingSeconds { get; private set; } = 180;

  /// <summary>
  /// Displays the email update verification page with a pre-filled email address.
  /// Also queries the real OTP TTL from Redis so the countdown timer is accurate.
  /// </summary>
  /// <param name="email">The new email address to verify.</param>
  /// <returns>The email verification page.</returns>
  public async Task<IActionResult> OnGetAsync(string email)
  {
    ViewModel = new VerifyEmailVm { Email = email };

    if (!string.IsNullOrWhiteSpace(email))
    {
      OtpRemainingSeconds = await _emailVerificationService.GetOtpRemainingSecondsAsync(email);
    }

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
      // Refresh the remaining seconds so the countdown UI stays accurate on re-render
      OtpRemainingSeconds = await _emailVerificationService.GetOtpRemainingSecondsAsync(ViewModel.Email);
      ModelState.AddModelError(string.Empty, codeError ?? "Verification failed.");
      return Page();
    }

    var pending = await _emailVerificationService.GetPendingEmailUpdateAsync(ViewModel.Email);
    if (pending is null)
    {
      OtpRemainingSeconds = 0;
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

  /// <summary>
  /// Re-sends a verification code for the email-update flow and returns JSON
  /// so the client can restart the countdown timer.
  /// </summary>
  /// <param name="email">The new email address that receives the verification code.</param>
  /// <returns>A JSON result containing resend status and cooldown information.</returns>
  public async Task<IActionResult> OnPostResendAsync([FromForm] string email)
  {
    if (string.IsNullOrWhiteSpace(email))
    {
      return new JsonResult(new { success = false, error = "Email is required.", remainingSeconds = 0 });
    }

    // Verify the pending email-update session still exists in Redis before resending
    var pending = await _emailVerificationService.GetPendingEmailUpdateAsync(email);
    if (pending is null)
    {
      return new JsonResult(new
      {
        success = false,
        error = "Email update session has expired. Please ask the administrator to update your email again.",
        remainingSeconds = 0
      });
    }

    var (success, error, remainingSeconds) = await _emailVerificationService.ResendCodeAsync(email);

    if (success)
    {
      // After a successful AJAX POST, ASP.NET Core may refresh the anti-forgery cookie.
      // If the cookie changes, the token already embedded in the verify form becomes stale,
      // which causes the next form submission to fail with 400 Bad Request.
      // Fix: generate fresh tokens here, set the updated cookie, and return the new form
      // token so the JavaScript can patch the verify form's hidden CSRF input immediately.
      var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
      return new JsonResult(new
      {
        success = true,
        error = (string?)null,
        remainingSeconds,
        csrfToken = tokens.RequestToken,
      });
    }

    return new JsonResult(new { success, error, remainingSeconds });
  }
}
