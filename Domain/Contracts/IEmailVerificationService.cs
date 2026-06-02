namespace Domain.Contracts;

/// <summary>
/// Manages OTP-based email verification flow with Redis-backed storage and rate limiting.
/// Coordinates code generation, delivery, and pending registration data lifecycle.
/// </summary>
public interface IEmailVerificationService
{
    /// <summary>
    /// Generates and sends a 6-digit OTP to the given email, then stores the pending
    /// registration data in Redis. Enforces a max of 5 sends per email per hour.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="fullName">The user's full name for the pending account.</param>
    /// <param name="password">The plain-text password — BCrypt-hashed before storage.</param>
    /// <returns>Success flag and an optional error message if rate-limited or send fails.</returns>
    Task<(bool Success, string? Error)> InitiateVerificationAsync(string email, string fullName, string password);

    /// <summary>
    /// Verifies the submitted OTP against the Redis-stored code for the given email.
    /// Tracks failed attempts and invalidates the code after 5 wrong entries.
    /// </summary>
    /// <param name="email">The email address being verified.</param>
    /// <param name="code">The 6-digit code submitted by the user.</param>
    /// <returns>Success flag and an optional error message on failure.</returns>
    Task<(bool Success, string? Error)> VerifyCodeAsync(string email, string code);

    /// <summary>
    /// Re-sends a new OTP to the email address. Subject to the same 5-per-hour rate limit.
    /// </summary>
    /// <param name="email">The email address to resend the code to.</param>
    /// <returns>Success flag, an optional error, and remaining cooldown seconds (always 180 on success).</returns>
    Task<(bool Success, string? Error, int RemainingSeconds)> ResendCodeAsync(string email);

    /// <summary>
    /// Retrieves the pending registration data stored in Redis for the given email.
    /// Returns <c>null</c> if the entry has expired or does not exist.
    /// </summary>
    /// <param name="email">The email address whose pending registration to retrieve.</param>
    Task<(string FullName, string BcryptHash)?> GetPendingRegistrationAsync(string email);

    /// <summary>
    /// Deletes the OTP and pending registration keys from Redis after successful account creation.
    /// </summary>
    /// <param name="email">The email address whose Redis keys should be removed.</param>
    Task CleanupAsync(string email);
}
