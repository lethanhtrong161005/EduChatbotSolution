namespace Domain.Contracts;

/// <summary>
/// Manages OTP-based email verification flow with Redis-backed storage and rate limiting.
/// Coordinates code generation, delivery, and pending registration data lifecycle.
/// Supports self-registration, email-update, and password-reset flows.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Generates a 6-digit OTP, BCrypt-hashes the password, stores both in Redis,
    /// then sends the verification email. Enforces 5 sends per email per hour.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="fullName">The user's full name for the pending account.</param>
    /// <param name="password">The plain-text password — BCrypt-hashed before Redis storage.</param>
    /// <returns>Success flag and an optional error message if rate-limited or send fails.</returns>
    Task<(bool Success, string? Error)> InitiateVerificationAsync(string email, string fullName, string password);

    /// <summary>
    /// Initiates an email-update verification flow. Stores the new email, user ID, and full name
    /// in Redis, then sends a verification email to the new address.
    /// </summary>
    /// <param name="newEmail">The new email address that must be verified.</param>
    /// <param name="fullName">The user's full name.</param>
    /// <param name="userId">The ID of the user whose email is being updated.</param>
    /// <returns>Success flag and an optional error message.</returns>
    Task<(bool Success, string? Error)> InitiateEmailUpdateVerificationAsync(
        string newEmail, string fullName, Guid userId);

    /// <summary>
    /// Initiates a password-reset verification flow for an existing account.
    /// Stores a pending reset marker in Redis and sends a 6-digit OTP.
    /// </summary>
    /// <param name="email">The account email address.</param>
    /// <param name="fullName">The user's full name.</param>
    /// <param name="userId">The ID of the account requesting password reset.</param>
    /// <returns>Success flag and an optional error message.</returns>
    Task<(bool Success, string? Error)> InitiatePasswordResetAsync(
        string email, string fullName, Guid userId);

    /// <summary>
    /// Validates the submitted OTP against the Redis-stored code. Increments the failed-attempt
    /// counter and deletes the code after 5 wrong entries.
    /// </summary>
    /// <param name="email">The email address being verified.</param>
    /// <param name="code">The 6-digit code submitted by the user.</param>
    /// <returns>Success flag and an optional error message on failure.</returns>
    Task<(bool Success, string? Error)> VerifyCodeAsync(string email, string code);

    /// <summary>
    /// Generates and sends a fresh OTP to the address. Subject to the same 5-per-hour rate limit.
    /// </summary>
    /// <param name="email">The email address to resend the code to.</param>
    /// <returns>Success flag, an optional error, and remaining cooldown seconds (180 on success).</returns>
    Task<(bool Success, string? Error, int RemainingSeconds)> ResendCodeAsync(string email);

    /// <summary>
    /// Retrieves pending self-registration data from Redis.
    /// Returns <c>null</c> if the entry has expired or does not exist.
    /// </summary>
    /// <param name="email">The email whose pending registration to retrieve.</param>
    Task<(string FullName, string BcryptHash)?> GetPendingRegistrationAsync(string email);

    /// <summary>
    /// Retrieves the pending email-update data from Redis.
    /// Returns <c>null</c> if the entry has expired or does not exist.
    /// </summary>
    /// <param name="newEmail">The new email whose pending update to retrieve.</param>
    Task<(Guid UserId, string FullName)?> GetPendingEmailUpdateAsync(string newEmail);

    /// <summary>
    /// Retrieves the pending password-reset data from Redis.
    /// Returns <c>null</c> if the entry has expired or does not exist.
    /// </summary>
    /// <param name="email">The email whose pending password reset to retrieve.</param>
    Task<(Guid UserId, string FullName)?> GetPendingPasswordResetAsync(string email);

    /// <summary>
    /// Removes OTP and pending registration keys from Redis after successful account creation.
    /// </summary>
    /// <param name="email">The email whose Redis keys should be removed.</param>
    Task CleanupAsync(string email);

    /// <summary>
    /// Removes OTP and pending email-update keys from Redis after email update succeeds.
    /// </summary>
    /// <param name="newEmail">The new email whose Redis keys should be removed.</param>
    Task CleanupEmailUpdateAsync(string newEmail);

    /// <summary>
    /// Removes password-reset OTP and pending reset keys after the password is changed.
    /// </summary>
    /// <param name="email">The email whose password-reset Redis keys should be removed.</param>
    Task CleanupPasswordResetAsync(string email);
}
