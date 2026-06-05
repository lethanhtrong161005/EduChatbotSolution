using Domain.Contracts;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Business.Services;

/// <summary>
/// Manages OTP lifecycle for all email-verification scenarios using Redis as the backing store.
/// Supports three distinct flows: self-registration, email-update, and password-reset.
/// Enforces a maximum of 5 send attempts per email per hour and 5 wrong-code entries per OTP.
/// </summary>
public class EmailVerificationService : IEmailVerificationService
{
    private const int OtpTtlSeconds = 180;
    private const int SendCountTtlSeconds = 3600;
    private const int MaxSendAttempts = 5;
    private const int MaxVerifyAttempts = 5;
    private const int PendingRegTtlSeconds = 1800;

    private readonly IDatabase _redis;
    private readonly IEmailService _emailService;
    private readonly ILogger<EmailVerificationService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EmailVerificationService"/>.
    /// </summary>
    /// <param name="multiplexer">The Redis connection multiplexer (singleton).</param>
    /// <param name="emailService">The SMTP email sender service.</param>
    /// <param name="logger">Logger for background task errors and successes.</param>
    public EmailVerificationService(IConnectionMultiplexer multiplexer, IEmailService emailService, ILogger<EmailVerificationService> logger)
    {
        _redis = multiplexer.GetDatabase();
        _emailService = emailService;
        _logger = logger;
    }

    // ── SELF-REGISTRATION FLOW ────────────────────────────────────

    /// <summary>
    /// Checks the send-rate limit, generates a 6-digit OTP, BCrypt-hashes the password,
    /// stores both the OTP and pending registration data in Redis, then sends the email.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="fullName">The user's full name for the pending account.</param>
    /// <param name="password">Plain-text password — hashed with BCrypt before Redis storage.</param>
    /// <returns>Success/Error tuple.</returns>
    public async Task<(bool Success, string? Error)> InitiateVerificationAsync(
        string email, string fullName, string password)
    {
        var (allowed, error) = await CheckAndIncrementSendCountAsync(email);
        if (!allowed) return (false, error);

        var code = GenerateCode();
        var bcryptHash = BCrypt.Net.BCrypt.HashPassword(password);

        var otpValue = JsonSerializer.Serialize(new OtpEntry(code, 0));
        await _redis.StringSetAsync(OtpKey(email), otpValue, TimeSpan.FromSeconds(OtpTtlSeconds));

        var pendingValue = JsonSerializer.Serialize(new PendingRegistration(fullName, bcryptHash));
        await _redis.StringSetAsync(PendingRegKey(email), pendingValue, TimeSpan.FromSeconds(PendingRegTtlSeconds));

        try
        {
            await _emailService.SendVerificationCodeAsync(email, fullName, code);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to send verification email: {ex.Message}");
        }
    }

    // ── EMAIL-UPDATE FLOW ─────────────────────────────────────────

    /// <summary>
    /// Initiates an email-update verification flow. Stores the new email, user ID, and full name
    /// in Redis under <c>pending_email_update:{newEmail}</c>, then sends a verification email.
    /// </summary>
    /// <param name="newEmail">The new email address that must be verified.</param>
    /// <param name="fullName">The user's full name.</param>
    /// <param name="userId">The ID of the user whose email is being updated.</param>
    /// <returns>Success/Error tuple.</returns>
    public async Task<(bool Success, string? Error)> InitiateEmailUpdateVerificationAsync(
        string newEmail, string fullName, Guid userId)
    {
        var (allowed, error) = await CheckAndIncrementSendCountAsync(newEmail);
        if (!allowed) return (false, error);

        var code = GenerateCode();

        var otpValue = JsonSerializer.Serialize(new OtpEntry(code, 0));
        await _redis.StringSetAsync(OtpKey(newEmail), otpValue, TimeSpan.FromSeconds(OtpTtlSeconds));

        var updatePending = JsonSerializer.Serialize(new PendingEmailUpdate(userId.ToString(), fullName));
        await _redis.StringSetAsync(
            PendingEmailUpdateKey(newEmail), updatePending, TimeSpan.FromSeconds(PendingRegTtlSeconds));

        try
        {
            await _emailService.SendEmailUpdateVerifyAsync(newEmail, fullName, code);
            _logger.LogInformation("Email-update verification email sent to {Email}", newEmail);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email-update verification email to {Email}", newEmail);
            return (false, $"Failed to send email-update verification: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the pending email-update data from Redis.
    /// </summary>
    /// <param name="newEmail">The new email whose pending update to retrieve.</param>
    public async Task<(Guid UserId, string FullName)?> GetPendingEmailUpdateAsync(string newEmail)
    {
        var raw = await _redis.StringGetAsync(PendingEmailUpdateKey(newEmail));
        if (!raw.HasValue) return null;

        var data = JsonSerializer.Deserialize<PendingEmailUpdate>(raw.ToString());
        if (data is null) return null;
        return Guid.TryParse(data.UserId, out var uid) ? (uid, data.FullName) : null;
    }

    // ── PASSWORD RESET FLOW ───────────────────────────────────────

    /// <summary>
    /// Initiates a password-reset verification flow. Stores the account ID and full name
    /// in Redis under <c>pending_password_reset:{email}</c>, then sends a reset-code email.
    /// </summary>
    /// <param name="email">The account email address.</param>
    /// <param name="fullName">The user's full name.</param>
    /// <param name="userId">The ID of the account requesting password reset.</param>
    /// <returns>Success/Error tuple.</returns>
    public async Task<(bool Success, string? Error)> InitiatePasswordResetAsync(
        string email, string fullName, Guid userId)
    {
        var (allowed, error) = await CheckAndIncrementSendCountAsync(email);
        if (!allowed) return (false, error);

        var code = GenerateCode();

        var otpValue = JsonSerializer.Serialize(new OtpEntry(code, 0));
        await _redis.StringSetAsync(OtpKey(email), otpValue, TimeSpan.FromSeconds(OtpTtlSeconds));

        var resetPending = JsonSerializer.Serialize(new PendingPasswordReset(userId.ToString(), fullName));
        await _redis.StringSetAsync(
            PendingPasswordResetKey(email), resetPending, TimeSpan.FromSeconds(PendingRegTtlSeconds));

        try
        {
            await _emailService.SendPasswordResetCodeAsync(email, fullName, code);
            _logger.LogInformation("Password-reset verification email sent to {Email}", email);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password-reset verification email to {Email}", email);
            return (false, $"Failed to send password-reset code: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves pending password-reset data from Redis.
    /// </summary>
    /// <param name="email">The email whose pending password reset to retrieve.</param>
    public async Task<(Guid UserId, string FullName)?> GetPendingPasswordResetAsync(string email)
    {
        var raw = await _redis.StringGetAsync(PendingPasswordResetKey(email));
        if (!raw.HasValue) return null;

        var data = JsonSerializer.Deserialize<PendingPasswordReset>(raw.ToString());
        if (data is null) return null;
        return Guid.TryParse(data.UserId, out var uid) ? (uid, data.FullName) : null;
    }

    // ── SHARED OTP VERIFY & RESEND ────────────────────────────────

    /// <summary>
    /// Validates the submitted code against the Redis-stored OTP.
    /// Increments the failed-attempt counter and deletes the OTP after 5 wrong entries.
    /// </summary>
    /// <param name="email">The email address being verified.</param>
    /// <param name="code">The 6-digit code submitted by the user.</param>
    /// <returns>Success/Error tuple.</returns>
    public async Task<(bool Success, string? Error)> VerifyCodeAsync(string email, string code)
    {
        var raw = await _redis.StringGetAsync(OtpKey(email));
        if (!raw.HasValue)
            return (false, "Verification code has expired or was not requested. Please request a new code.");

        var entry = JsonSerializer.Deserialize<OtpEntry>(raw.ToString())
                    ?? throw new InvalidOperationException("Corrupt OTP entry in Redis.");

        if (entry.Code != code)
        {
            var newAttempts = entry.VerifyAttempts + 1;
            if (newAttempts >= MaxVerifyAttempts)
            {
                await _redis.KeyDeleteAsync(OtpKey(email));
                return (false, "Too many incorrect attempts. Please request a new verification code.");
            }

            var ttl = await _redis.KeyTimeToLiveAsync(OtpKey(email));
            var updatedEntry = JsonSerializer.Serialize(new OtpEntry(entry.Code, newAttempts));
            if (ttl.HasValue)
                await _redis.StringSetAsync(OtpKey(email), updatedEntry, ttl.Value);
            else
                await _redis.StringSetAsync(OtpKey(email), updatedEntry);

            var remaining = MaxVerifyAttempts - newAttempts;
            return (false, $"Incorrect code. You have {remaining} attempt(s) remaining.");
        }

        return (true, null);
    }

    /// <summary>
    /// Generates and sends a fresh OTP, enforcing the same 5-per-hour send-rate limit.
    /// Determines which email template to use based on which pending key exists.
    /// </summary>
    /// <param name="email">The email address to resend the code to.</param>
    /// <returns>Success/Error/RemainingSeconds tuple.</returns>
    public async Task<(bool Success, string? Error, int RemainingSeconds)> ResendCodeAsync(string email)
    {
        // 1. Determine pending context (self-reg vs email-update vs password-reset)
        var pendingRaw = await _redis.StringGetAsync(PendingRegKey(email));
        var emailUpdateRaw = await _redis.StringGetAsync(PendingEmailUpdateKey(email));
        var passwordResetRaw = await _redis.StringGetAsync(PendingPasswordResetKey(email));

        if (!pendingRaw.HasValue && !emailUpdateRaw.HasValue && !passwordResetRaw.HasValue)
            return (false, "Session has expired. Please restart the process.", 0);

        var (allowed, error) = await CheckAndIncrementSendCountAsync(email);
        if (!allowed) return (false, error, 0);

        var code = GenerateCode();
        var otpValue = JsonSerializer.Serialize(new OtpEntry(code, 0));
        await _redis.StringSetAsync(OtpKey(email), otpValue, TimeSpan.FromSeconds(OtpTtlSeconds));

        try
        {
            string name = "User";
            if (pendingRaw.HasValue)
            {
                var p = JsonSerializer.Deserialize<PendingRegistration>(pendingRaw.ToString());
                name = p?.FullName ?? name;
                await _emailService.SendVerificationCodeAsync(email, name, code);
            }
            else if (emailUpdateRaw.HasValue)
            {
                var p = JsonSerializer.Deserialize<PendingEmailUpdate>(emailUpdateRaw.ToString());
                name = p?.FullName ?? name;
                await _emailService.SendEmailUpdateVerifyAsync(email, name, code);
            }
            else
            {
                var p = JsonSerializer.Deserialize<PendingPasswordReset>(passwordResetRaw.ToString());
                name = p?.FullName ?? name;
                await _emailService.SendPasswordResetCodeAsync(email, name, code);
            }
            return (true, null, OtpTtlSeconds);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to resend verification email: {ex.Message}", 0);
        }
    }

    // ── READ HELPERS ──────────────────────────────────────────────

    /// <summary>Retrieves self-registration pending data from Redis.</summary>
    /// <param name="email">The email whose pending registration to retrieve.</param>
    public async Task<(string FullName, string BcryptHash)?> GetPendingRegistrationAsync(string email)
    {
        var raw = await _redis.StringGetAsync(PendingRegKey(email));
        if (!raw.HasValue) return null;
        var data = JsonSerializer.Deserialize<PendingRegistration>(raw.ToString());
        return data is null ? null : (data.FullName, data.BcryptHash);
    }

    // ── CLEANUP ───────────────────────────────────────────────────

    /// <summary>Removes self-registration OTP and pending registration keys.</summary>
    /// <param name="email">The email whose Redis keys should be removed.</param>
    public async Task CleanupAsync(string email)
    {
        await Task.WhenAll(
            _redis.KeyDeleteAsync(OtpKey(email)),
            _redis.KeyDeleteAsync(PendingRegKey(email)));
    }

    /// <summary>Removes email-update OTP and pending email-update keys.</summary>
    /// <param name="newEmail">The new email whose Redis keys should be removed.</param>
    public async Task CleanupEmailUpdateAsync(string newEmail)
    {
        await Task.WhenAll(
            _redis.KeyDeleteAsync(OtpKey(newEmail)),
            _redis.KeyDeleteAsync(PendingEmailUpdateKey(newEmail)));
    }

    /// <summary>Removes password-reset OTP and pending reset keys.</summary>
    /// <param name="email">The email whose password-reset Redis keys should be removed.</param>
    public async Task CleanupPasswordResetAsync(string email)
    {
        await Task.WhenAll(
            _redis.KeyDeleteAsync(OtpKey(email)),
            _redis.KeyDeleteAsync(PendingPasswordResetKey(email)));
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────

    /// <summary>
    /// Increments the send counter and returns whether the send is allowed.
    /// Counter TTL is set on the first increment (1-hour fixed window).
    /// </summary>
    /// <param name="email">The email address for the rate-limit key.</param>
    private async Task<(bool Allowed, string? Error)> CheckAndIncrementSendCountAsync(string email)
    {
        var countKey = SendCountKey(email);
        var count = await _redis.StringIncrementAsync(countKey);

        if (count == 1)
            await _redis.KeyExpireAsync(countKey, TimeSpan.FromSeconds(SendCountTtlSeconds));

        if (count > MaxSendAttempts)
        {
            var ttl = await _redis.KeyTimeToLiveAsync(countKey);
            var waitMinutes = (int)Math.Ceiling((ttl ?? TimeSpan.Zero).TotalMinutes);
            return (false,
                $"Too many verification code requests. Please wait approximately {waitMinutes} minute(s) before trying again.");
        }

        return (true, null);
    }

    private static string GenerateCode() => Random.Shared.Next(100_000, 999_999).ToString();

    private static string OtpKey(string email) => $"otp:{email.ToLowerInvariant()}";
    private static string SendCountKey(string email) => $"otp:send_count:{email.ToLowerInvariant()}";
    private static string PendingRegKey(string email) => $"pending_reg:{email.ToLowerInvariant()}";
    private static string PendingEmailUpdateKey(string email) => $"pending_email_update:{email.ToLowerInvariant()}";
    private static string PendingPasswordResetKey(string email) => $"pending_password_reset:{email.ToLowerInvariant()}";

    // ── Private record types ──────────────────────────────────────

    private record OtpEntry(string Code, int VerifyAttempts);
    private record PendingRegistration(string FullName, string BcryptHash);
    private record PendingEmailUpdate(string UserId, string FullName);
    private record PendingPasswordReset(string UserId, string FullName);
}
