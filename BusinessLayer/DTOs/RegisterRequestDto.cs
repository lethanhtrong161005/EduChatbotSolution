using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs;

/// <summary>
/// Data Transfer Object for user registration requests.
/// Contains information required to create a new user account.
/// </summary>
public class RegisterRequestDto
{
    /// <summary>
    /// The full name of the new user.
    /// Must be between 2 and 150 characters.
    /// </summary>
    [Required(ErrorMessage = "Full name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 150 characters.")]
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The email address for the new user account.
    /// Must be a valid email format and unique across the system.
    /// </summary>
    [Required(ErrorMessage = "Email address is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The plaintext password for the new user account.
    /// Will be hashed before storage. Must meet complexity requirements.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [StringLength(int.MaxValue, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one digit.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The confirmation password for verification during registration.
    /// Must exactly match the Password field.
    /// </summary>
    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
