using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

/// <summary>
/// View model for requesting a password-reset verification code.
/// Carries the email address entered on the forgot-password page.
/// </summary>
public class ForgotPasswordVm
{
    /// <summary>
    /// The account email address that should receive the password-reset code.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
