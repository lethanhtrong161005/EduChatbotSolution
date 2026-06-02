using Domain.Contracts;
using StackExchange.Redis;
using System.Text.Json;

namespace Business.Services;

/// <summary>
/// Manages OTP lifecycle for all email-verification scenarios using Redis as the backing store.
/// Supports three distinct flows: self-registration, admin-created accounts, and email-update.
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

    /// <summary>
    /// Initializes a new instance of <see cref="EmailVerificationService"/>.
    /// </summary>
    /// <param name="multiplexer">The Redis connection multiplexer (singleton).</param>
    /// <param name="emailService">The SMTP email sender service.</param>
    public EmailVerificationService(IConnectionMultiplexer multiplexer, IEmailService emailService)
    {
        _redis = multiplexer.GetDatabase();
        _emailService = emailService;
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

    // ── ADMIN-CREATED ACCOUNT FLOW ────────────────────────────────

    /// <summary>
    /// Initiates the admin-created account verification flow. Stores admin pending data
    /// (role + plain-text password for the welcome email) under a separate Redis key prefix,
    /// then sends the admin-flavored verification email.
    /// </summary>
    /// <param name="email">The email address to verify.</param>
    /// <param name="fullName">The user's full name.</param>
    /// <param name="role">The role assigned by the admin.</param>
    /// <param name="plainPassword">The plain-text password stored temporarily for the welcome email.</param>
    /// <returns>Success/Error tuple.</returns>
    public async Task<(bool Success, string? Error)> InitiateAdminVerificationAsync(
        string email, string fullName, string role, string plainPassword)
    {
        var (allowed, error) = await CheckAndIncrementSendCountAsync(email);
        if (!allowed) return (false, error);

        var code = GenerateCode();
        var bcryptHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        // OTP key reuses the same namespace (same verify-code page)
        var otpValue = JsonSerializer.Serialize(new OtpEntry(code, 0));
        await _redis.StringSetAsync(OtpKey(email), otpValue, TimeSpan.FromSeconds(OtpTtlSeconds));

        // Admin pending key carries role + plain password
        var adminPending = JsonSerializer.Serialize(
            new AdminPendingRegistration(fullName, bcryptHash, role, plainPassword));
        await _redis.StringSetAsync(
            PendingAdminRegKey(email), adminPending, TimeSpan.FromSeconds(PendingRegTtlSeconds));

        try
        {
            await _emailService.SendAdminCreatedVerifyAsync(email, fullName, code);
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to send verification email: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves admin-created pending registration data from Redis.
    /// </summary>
    /// <param name="email">The email whose pending admin registration to retrieve.</param>
    public async Task<(string FullName, string BcryptHash, string Role, string PlainPassword)?> GetPendingAdminRegistrationAsync(string email)
    {
        var raw = await _redis.StringGetAsync(PendingAdminRegKey(email));
        if (!raw.HasValue) return null;

        var data = JsonSerializer.Deserialize<AdminPendingRegistration>(raw.ToString());
        return data is null ? null : (data.FullName, data.BcryptHash, data.Role, data.PlainPassword);
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
            return (true, null);
        }
        catch (Exception ex)
        {
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
        // 1. Determine pending context (self-reg vs admin-created vs email-update)
        var pendingRaw = await _redis.StringGetAsync(PendingRegKey(email));
        var adminPendingRaw = await _redis.StringGetAsync(PendingAdminRegKey(email));
        var emailUpdateRaw = await _redis.StringGetAsync(PendingEmailUpdateKey(email));

        if (!pendingRaw.HasValue && !adminPendingRaw.HasValue && !emailUpdateRaw.HasValue)
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
            else if (adminPendingRaw.HasValue)
            {
                var p = JsonSerializer.Deserialize<AdminPendingRegistration>(adminPendingRaw.ToString());
                name = p?.FullName ?? name;
                await _emailService.SendAdminCreatedVerifyAsync(email, name, code);
            }
            else
            {
                var p = JsonSerializer.Deserialize<PendingEmailUpdate>(emailUpdateRaw.ToString());
                name = p?.FullName ?? name;
                await _emailService.SendEmailUpdateVerifyAsync(email, name, code);
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
            _redis.KeyDeleteAsync(PendingRegKey(email)),
            _redis.KeyDeleteAsync(PendingAdminRegKey(email)));
    }

    /// <summary>Removes email-update OTP and pending email-update keys.</summary>
    /// <param name="newEmail">The new email whose Redis keys should be removed.</param>
    public async Task CleanupEmailUpdateAsync(string newEmail)
    {
        await Task.WhenAll(
            _redis.KeyDeleteAsync(OtpKey(newEmail)),
            _redis.KeyDeleteAsync(PendingEmailUpdateKey(newEmail)));
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
    private static string PendingAdminRegKey(string email) => $"pending_admin_reg:{email.ToLowerInvariant()}";
    private static string PendingEmailUpdateKey(string email) => $"pending_email_update:{email.ToLowerInvariant()}";

    // ── Private record types ──────────────────────────────────────

    private record OtpEntry(string Code, int VerifyAttempts);
    private record PendingRegistration(string FullName, string BcryptHash);
    private record AdminPendingRegistration(string FullName, string BcryptHash, string Role, string PlainPassword);
    private record PendingEmailUpdate(string UserId, string FullName);
}
