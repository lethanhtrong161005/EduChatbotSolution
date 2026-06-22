using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Constants;
using Presentation.ViewModels;
using System.Security.Claims;

namespace Presentation.Pages.Account;

/// <summary>
/// Handles the login page and authenticates users with email and password credentials.
/// </summary>
[AllowAnonymous]
public class LoginModel(
    IAuthService authService,
    SignInManager<ApplicationUser> signInManager) : PageModel
{
    private readonly IAuthService _authService = authService;
    private readonly SignInManager<ApplicationUser> _signInManager = signInManager;

    /// <summary>
    /// Gets the login form data rendered by the page.
    /// </summary>
    public LoginRequestVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Gets the URL that receives the user after successful login.
    /// </summary>
    public string ReturnUrl { get; private set; } = AuthenticationConstants.FallbackReturnUrl;

    /// <summary>
    /// Displays the login page or redirects an already authenticated user.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful login.</param>
    /// <returns>The login page or a redirect result.</returns>
    public IActionResult OnGet(string returnUrl = AuthenticationConstants.FallbackReturnUrl)
    {
        ReturnUrl = string.IsNullOrWhiteSpace(returnUrl)
            ? AuthenticationConstants.FallbackReturnUrl
            : returnUrl;

        if (User.Identity?.IsAuthenticated == true)
        {
            return LocalRedirect(ReturnUrl);
        }

        return Page();
    }

    /// <summary>
    /// Processes submitted login credentials and issues an authentication cookie.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful login.</param>
    /// <returns>A redirect result on success or the login page with validation errors.</returns>
    public async Task<IActionResult> OnPostAsync(
        [Bind(Prefix = nameof(ViewModel))] LoginRequestVm viewModel,
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

        var loginResult = await _authService.LoginAsync(ViewModel.Email, ViewModel.Password);

        if (loginResult.Success)
        {
            var authProps = new AuthenticationProperties
            {
                IsPersistent = ViewModel.RememberMe,
                ExpiresUtc = ViewModel.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(30)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await _signInManager.SignInWithClaimsAsync(loginResult.User, authProps, loginResult.Claims);

            var isAdmin = loginResult.Claims.Any(c =>
                c.Type == ClaimTypes.Role && c.Value == UserRole.Admin.ToString());

            if (isAdmin)
            {
                return Redirect("/admin/user-manage");
            }

            return LocalRedirect(ReturnUrl);
        }

        ModelState.AddModelError(string.Empty, loginResult.Errors.FirstOrDefault() ?? AppConstants.InvalidCredentials);
        return Page();
    }
}
