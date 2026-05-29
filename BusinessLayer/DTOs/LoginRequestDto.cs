namespace BusinessLayer.DTOs;

/// <summary>
/// Data Transfer Object for user login requests.
/// Contains credentials required for user authentication.
/// </summary>
public class LoginRequestDto
{
    /// <summary>
    /// The email address of the user attempting to log in.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The plaintext password provided by the user for authentication.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the user wants to remain logged in across sessions.
    /// </summary>
    public bool RememberMe { get; set; }
}
