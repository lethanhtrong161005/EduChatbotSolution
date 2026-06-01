using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

/// <summary>
/// Data transfer object for user login requests.
/// Contains the credentials required for email/password authentication.
/// </summary>
public class LoginRequestVm
{
    /// <summary>
    /// The email address used to identify the user account.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The password associated with the user account.
    /// </summary>
    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the authentication cookie should persist across browser sessions.
    /// </summary>
    public bool RememberMe { get; set; }
}
