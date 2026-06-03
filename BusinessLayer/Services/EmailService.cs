using Domain.Contracts;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Reflection;

namespace Business.Services;

/// <summary>
/// Sends transactional emails via Gmail SMTP using MailKit.
/// All HTML templates are bundled as embedded resources in this assembly,
/// eliminating the need for file-system path resolution at runtime.
/// </summary>
public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _senderEmail;
    private readonly string _senderName;
    private readonly string _appPassword;
    private readonly ILogger<EmailService> _logger;

    private const string VerifyCodeTemplate = "Business.Templates.EmailVerificationCode.html";
    private const string AdminVerifyTemplate = "Business.Templates.AdminCreatedVerifyEmail.html";
    private const string WelcomePasswordTemplate = "Business.Templates.WelcomeWithPassword.html";
    private const string EmailUpdateTemplate = "Business.Templates.EmailUpdateVerify.html";
    private const string AccountDisabledTemplate = "Business.Templates.AccountDisabled.html";
    private const string AccountDeletedTemplate = "Business.Templates.AccountDeleted.html";

    private readonly string _appBaseUrl;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _logger = logger;
        _smtpHost = configuration["Email:SmtpHost"] ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");
        _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        _senderEmail = configuration["Email:SenderEmail"] ?? throw new InvalidOperationException("Email:SenderEmail is not configured.");
        _senderName = configuration["Email:SenderName"] ?? "EduChatAI";
        _appPassword = configuration["Email:AppPassword"] ?? throw new InvalidOperationException("Email:AppPassword is not configured.");
        _appBaseUrl = configuration["AppBaseUrl"] ?? "https://educhatai.com";
    }

    // ── SELF-REGISTRATION OTP ──────────────────────────────────────

    /// <summary>
    /// Sends the self-registration OTP email using the standard verification template.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="toName">The recipient's display name.</param>
    /// <param name="code">The 6-digit OTP substituted into the <c>{{CODE}}</c> placeholder.</param>
    public async Task SendVerificationCodeAsync(string toEmail, string toName, string code)
    {
        var html = (await LoadTemplateAsync(VerifyCodeTemplate)).Replace("{{CODE}}", code);
        await SendAsync(toEmail, toName, "EduChatAI – Mã xác nhận của bạn", html);
    }

    // ── ADMIN-CREATED ACCOUNT FLOWS ───────────────────────────────

    /// <summary>
    /// Sends the verification email for an account created by an admin.
    /// Uses the admin-flavored template to set expectations for the new user.
    /// </summary>
    /// <param name="toEmail">The new account's email address.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="code">The 6-digit OTP to embed.</param>
    public async Task SendAdminCreatedVerifyAsync(string toEmail, string toName, string code)
    {
        var verifyUrl = $"{_appBaseUrl}/verify-email?email={Uri.EscapeDataString(toEmail)}";
        var html = (await LoadTemplateAsync(AdminVerifyTemplate))
            .Replace("{{NAME}}", toName)
            .Replace("{{CODE}}", code)
            .Replace("{{VERIFY_URL}}", verifyUrl);
        await SendAsync(toEmail, toName, "EduChatAI – Activate Your Account", html);
    }

    /// <summary>
    /// Sends the welcome email with the plain-text password after a successful admin-created verification.
    /// </summary>
    /// <param name="toEmail">The verified email address.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="plainPassword">The plain-text password to include in the email body.</param>
    public async Task SendWelcomeWithPasswordAsync(string toEmail, string toName, string plainPassword)
    {
        var loginUrl = $"{_appBaseUrl}/login";
        var html = (await LoadTemplateAsync(WelcomePasswordTemplate))
            .Replace("{{NAME}}", toName)
            .Replace("{{EMAIL}}", toEmail)
            .Replace("{{PASSWORD}}", plainPassword)
            .Replace("{{LOGIN_URL}}", loginUrl);
        await SendAsync(toEmail, toName, "Welcome to EduChatAI! 🎉", html);
    }

    // ── EMAIL-UPDATE FLOW ──────────────────────────────────────────

    /// <summary>
    /// Sends a verification email to the new address when an admin updates a user's email.
    /// </summary>
    /// <param name="toEmail">The new email address being verified.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="code">The 6-digit OTP to embed.</param>
    public async Task SendEmailUpdateVerifyAsync(string toEmail, string toName, string code)
    {
        var html = (await LoadTemplateAsync(EmailUpdateTemplate))
            .Replace("{{NAME}}", toName)
            .Replace("{{CODE}}", code);
        await SendAsync(toEmail, toName, "EduChatAI – Verify Your New Email", html);
    }

    // ── LIFECYCLE NOTIFICATIONS ────────────────────────────────────

    /// <summary>
    /// Notifies a user that their account has been disabled by an administrator.
    /// </summary>
    /// <param name="toEmail">The user's current email address.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="contactEmail">The support contact email to include in the email body.</param>
    public async Task SendAccountDisabledAsync(string toEmail, string toName, string contactEmail)
    {
        var html = (await LoadTemplateAsync(AccountDisabledTemplate))
            .Replace("{{NAME}}", toName)
            .Replace("{{CONTACT_EMAIL}}", contactEmail);
        await SendAsync(toEmail, toName, "EduChatAI – Your Account Has Been Disabled", html);
    }

    /// <summary>
    /// Notifies a user that their account has been soft-deleted by an administrator.
    /// </summary>
    /// <param name="toEmail">The user's email address at the time of deletion.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="contactEmail">The support contact email to include in the email body.</param>
    public async Task SendAccountDeletedAsync(string toEmail, string toName, string contactEmail)
    {
        var html = (await LoadTemplateAsync(AccountDeletedTemplate))
            .Replace("{{NAME}}", toName)
            .Replace("{{CONTACT_EMAIL}}", contactEmail);
        await SendAsync(toEmail, toName, "EduChatAI – Your Account Has Been Removed", html);
    }

    // ── PRIVATE HELPERS ───────────────────────────────────────────

    /// <summary>
    /// Reads an HTML template from the embedded assembly resource by resource name.
    /// </summary>
    /// <param name="resourceName">The fully-qualified manifest resource name.</param>
    /// <returns>The raw HTML string.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the resource is not found.</exception>
    private static async Task<string> LoadTemplateAsync(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Embedded email template '{resourceName}' not found. " +
                $"Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    /// <summary>
    /// Builds a MimeMessage and sends it via Gmail SMTP with STARTTLS.
    /// </summary>
    /// <param name="toEmail">Recipient email address.</param>
    /// <param name="toName">Recipient display name.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="htmlBody">Pre-built HTML email body.</param>
    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        _logger.LogInformation("Sending email to {Email} | Subject: {Subject} | Host: {Host}:{Port}",
            toEmail, subject, _smtpHost, _smtpPort);
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_senderName, _senderEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_senderEmail, _appPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(quit: true);

            _logger.LogInformation("Email delivered successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email} | {ExType}: {Message}",
                toEmail, ex.GetType().Name, ex.Message);
            throw;
        }
    }
}
