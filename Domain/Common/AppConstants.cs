namespace Domain.Common;

/// <summary>
/// Provides application-wide constant values used across all layers.
/// Centralizes role names, TempData keys, and validation messages to avoid magic strings.
/// </summary>
public static class AppConstants
{
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

    /// <summary>Success message shown after a successful registration.</summary>
    public const string RegistrationSuccess = "Registration successful! Please sign in with your new account.";

    /// <summary>Error message shown when the user account is disabled by an administrator.</summary>
    public const string AccountDisabled = "Your account has been disabled. Please contact the administrator.";

    public const int UnlimitedQuota = -1;
}
