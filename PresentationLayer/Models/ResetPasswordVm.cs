using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

/// <summary>
/// View model for completing a password reset after OTP verification.
/// Carries the email address, verification code, and new password fields.
/// </summary>
public class ResetPasswordVm
{
    /// <summary>
    /// The account email address being reset.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The 6-digit OTP code sent to the account email address.
    /// </summary>
    [Required(ErrorMessage = "Please enter the verification code.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be exactly 6 digits.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Code must contain only digits.")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// The new password to save after the code is verified.
    /// </summary>
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation field that must match the new password exactly.
    /// </summary>
    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
