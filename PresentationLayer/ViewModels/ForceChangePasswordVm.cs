using System.ComponentModel.DataAnnotations;

namespace Presentation.ViewModels;

/// <summary>
/// View model for the mandatory password change page shown to admin-created users
/// on their first login.
/// </summary>
public class ForceChangePasswordVm
{
    /// <summary>
    /// The account email address (passed via TempData from the login page).
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The current auto-generated password the user received via email.
    /// </summary>
    [Required(ErrorMessage = "Current password is required.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// The new password chosen by the user.
    /// </summary>
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    /// <summary>
    /// Confirmation field that must match the new password exactly.
    /// </summary>
    [Required(ErrorMessage = "Please confirm your new password.")]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
