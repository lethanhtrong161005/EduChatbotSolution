using Domain.Contracts;
using StackExchange.Redis;
using System.Text.Json;

namespace Business.Services;

/// <summary>
/// Manages the full OTP lifecycle for email verification using Redis as the backing store.
/// Enforces a maximum of 5 send attempts per email per hour and 5 wrong code entries per OTP.
/// </summary>
public class EmailVerificationService : IEmailVerificationService
{
    private const int OtpTtlSeconds = 180;           // 3-minute code lifetime
    private const int SendCountTtlSeconds = 3600;    // 1-hour send-rate window
    private const int MaxSendAttempts = 5;
    private const int MaxVerifyAttempts = 5;
    private const int PendingRegTtlSeconds = 1800;   // 30-minute pending registration window

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

        // Store OTP
        var otpValue = JsonSerializer.Serialize(new OtpEntry(code, 0));
        await _redis.StringSetAsync(OtpKey(email), otpValue, TimeSpan.FromSeconds(OtpTtlSeconds));

        // Store pending registration (30-minute window)
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

    /// <summary>
    /// Validates the submitted code against the Redis-stored OTP.
    /// Increments failed-attempt counter and deletes the OTP after 5 wrong entries.
    /// </summary>
    /// <param name="email">The email address being verified.</param>
    /// <param name="code">The 6-digit code submitted by the user.</param>
    /// <returns>Success/Error tuple.</returns>
    public async Task<(bool Success, string? Error)> VerifyCodeAsync(string email, string code)
    {
        var raw = await _redis.StringGetAsync(OtpKey(email));
        if (!raw.HasValue)
        {
            return (false, "Verification code has expired or was not requested. Please request a new code.");
        }

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

            // Update attempt count preserving remaining TTL
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
    /// </summary>
    /// <param name="email">The email address to resend the code to.</param>
    /// <returns>Success/Error/RemainingSeconds tuple.</returns>
    public async Task<(bool Success, string? Error, int RemainingSeconds)> ResendCodeAsync(string email)
    {
        var pendingRaw = await _redis.StringGetAsync(PendingRegKey(email));
        if (!pendingRaw.HasValue)
        {
            return (false, "Registration session has expired. Please restart the registration process.", 0);
        }

        var pending = JsonSerializer.Deserialize<PendingRegistration>(pendingRaw.ToString())!;

        var (allowed, error) = await CheckAndIncrementSendCountAsync(email);
        if (!allowed) return (false, error, 0);

        var code = GenerateCode();
        var otpValue = JsonSerializer.Serialize(new OtpEntry(code, 0));
        await _redis.StringSetAsync(OtpKey(email), otpValue, TimeSpan.FromSeconds(OtpTtlSeconds));

        try
        {
            await _emailService.SendVerificationCodeAsync(email, pending.FullName, code);
            return (true, null, OtpTtlSeconds);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to resend verification email: {ex.Message}", 0);
        }
    }

    /// <summary>
    /// Retrieves the pending registration data stored when the OTP was first sent.
    /// Returns <c>null</c> if the entry has expired.
    /// </summary>
    /// <param name="email">The email address whose pending registration to look up.</param>
    public async Task<(string FullName, string BcryptHash)?> GetPendingRegistrationAsync(string email)
    {
        var raw = await _redis.StringGetAsync(PendingRegKey(email));
        if (!raw.HasValue) return null;

        var data = JsonSerializer.Deserialize<PendingRegistration>(raw.ToString());
        return data is null ? null : (data.FullName, data.BcryptHash);
    }

    /// <summary>
    /// Removes the OTP and pending registration keys from Redis after account creation.
    /// </summary>
    /// <param name="email">The email address whose keys to delete.</param>
    public async Task CleanupAsync(string email)
    {
        await Task.WhenAll(
            _redis.KeyDeleteAsync(OtpKey(email)),
            _redis.KeyDeleteAsync(PendingRegKey(email))
        );
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Increments the send counter for the email and returns whether the send is allowed.
    /// Counter TTL is set on first increment (1-hour fixed window).
    /// </summary>
    private async Task<(bool Allowed, string? Error)> CheckAndIncrementSendCountAsync(string email)
    {
        var countKey = SendCountKey(email);
        var count = await _redis.StringIncrementAsync(countKey);

        if (count == 1)
        {
            // First send — start the 1-hour window
            await _redis.KeyExpireAsync(countKey, TimeSpan.FromSeconds(SendCountTtlSeconds));
        }

        if (count > MaxSendAttempts)
        {
            var ttl = await _redis.KeyTimeToLiveAsync(countKey);
            var waitMinutes = (int)Math.Ceiling((ttl ?? TimeSpan.Zero).TotalMinutes);
            return (false,
                $"Too many verification code requests. Please wait approximately {waitMinutes} minute(s) before trying again.");
        }

        return (true, null);
    }

    private static string GenerateCode() =>
        Random.Shared.Next(100_000, 999_999).ToString();

    private static string OtpKey(string email) => $"otp:{email.ToLowerInvariant()}";
    private static string SendCountKey(string email) => $"otp:send_count:{email.ToLowerInvariant()}";
    private static string PendingRegKey(string email) => $"pending_reg:{email.ToLowerInvariant()}";

    // ── Private record types ──────────────────────────────────────

    private record OtpEntry(string Code, int VerifyAttempts);
    private record PendingRegistration(string FullName, string BcryptHash);
}
