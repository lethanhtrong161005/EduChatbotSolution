using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

/// <summary>
/// View model for the email verification page. Carries the target email
/// and the 6-digit code submitted by the user.
/// </summary>
public class VerifyEmailVm
{
    /// <summary>
    /// The email address being verified. Passed via route/query and preserved across form submissions.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The 6-digit OTP code entered by the user.
    /// Populated client-side by concatenating the six individual digit inputs.
    /// </summary>
    [Required(ErrorMessage = "Please enter the verification code.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must contain only digits.")]
    public string Code { get; set; } = string.Empty;
}
