namespace BusinessLayer.Services.Implementations;

/// <summary>
/// Provides application-wide constant values used across the Business and Presentation layers.
/// Centralizes role names, TempData keys, and validation messages to avoid magic strings.
/// </summary>
public static class AppConstants
{
    // ── Role Names ───────────────────────────────────────────
    /// <summary>
    /// The administrator role name with full system access.
    /// </summary>
    public const string RoleAdmin = "Admin";

    /// <summary>
    /// The lecturer role name for course management.
    /// </summary>
    public const string RoleLecturer = "Lecturer";

    /// <summary>
    /// The student role name assigned by default upon registration.
    /// </summary>
    public const string RoleStudent = "Student";

    // ── TempData Keys ────────────────────────────────────────
    /// <summary>
    /// TempData key for success messages displayed after redirect.
    /// </summary>
    public const string TempDataSuccess = "SuccessMessage";

    /// <summary>
    /// TempData key for error messages displayed after redirect.
    /// </summary>
    public const string TempDataError = "ErrorMessage";

    // ── Authentication Messages ──────────────────────────────
    /// <summary>
    /// Error message shown when login credentials are invalid.
    /// </summary>
    public const string InvalidCredentials = "Invalid email or password. Please try again.";

    /// <summary>
    /// Error message shown when the user account is locked out.
    /// </summary>
    public const string AccountLockedOut = "Your account has been locked. Please try again later.";

    /// <summary>
    /// Error message shown when the user account is not allowed to sign in.
    /// </summary>
    public const string AccountNotAllowed = "Your account is not allowed to sign in. Please verify your email.";

    /// <summary>
    /// Success message shown after a successful registration.
    /// </summary>
    public const string RegistrationSuccess = "Registration successful! Please sign in with your new account.";
}
