using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs;

/// <summary>
/// Data transfer object for new user registration requests.
/// Contains all fields required to create a new user account.
/// </summary>
public class RegisterRequestDto
{
    /// <summary>
    /// The email address for the new user account. Must be unique in the system.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the user shown throughout the application.
    /// </summary>
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The password for the new account. Must meet the configured password policy.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation field. Must exactly match the Password field.
    /// </summary>
    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
