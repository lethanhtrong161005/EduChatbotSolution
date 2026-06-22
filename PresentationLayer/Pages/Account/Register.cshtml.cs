using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Constants;
using Presentation.ViewModels;

namespace Presentation.Pages.Account;

/// <summary>
/// Handles the registration page and starts the email verification flow.
/// </summary>
[AllowAnonymous]
public class RegisterModel(
    IAuthService authService,
    IEmailVerificationService emailVerificationService) : PageModel
{
    private readonly IAuthService _authService = authService;
    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;

    /// <summary>
    /// Gets the registration form data rendered by the page.
    /// </summary>
    public RegisterRequestVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Gets the URL that receives the user after successful login.
    /// </summary>
    public string ReturnUrl { get; private set; } = AuthenticationConstants.FallbackReturnUrl;

    /// <summary>
    /// Displays the registration page for anonymous users.
    /// </summary>
    /// <param name="returnUrl">The URL to preserve through the registration flow.</param>
    /// <returns>The registration page or a redirect for authenticated users.</returns>
    public IActionResult OnGet(string returnUrl = AuthenticationConstants.FallbackReturnUrl)
    {
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? AuthenticationConstants.FallbackReturnUrl
            : returnUrl;

        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(AuthenticationConstants.FallbackReturnUrl);
        }

        return Page();
    }

    /// <summary>
    /// Validates the registration form and sends a verification code to the user.
    /// </summary>
    /// <param name="returnUrl">The URL to preserve through the registration flow.</param>
    /// <returns>A redirect to verification or the page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(
        [Bind(Prefix = nameof(ViewModel))] RegisterRequestVm viewModel,
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

        if (await _authService.EmailExistsAsync(ViewModel.Email))
        {
            ModelState.AddModelError(string.Empty, "An account with this email address already exists.");
            return Page();
        }

        var (success, error) = await _emailVerificationService.InitiateVerificationAsync(
            ViewModel.Email,
            ViewModel.FullName,
            ViewModel.Password);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, error ?? "Failed to send verification code.");
            return Page();
        }

        return RedirectToPage("/Account/VerifyEmail", new { email = ViewModel.Email, returnUrl = ReturnUrl });
    }
}
