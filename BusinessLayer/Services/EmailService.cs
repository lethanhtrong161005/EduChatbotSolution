using Domain.Contracts;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using System.Reflection;

namespace Business.Services;

/// <summary>
/// Sends transactional emails via Gmail SMTP using MailKit.
/// The HTML template is bundled as an embedded resource inside this assembly,
/// so no file-system path resolution is required at runtime.
/// </summary>
public class EmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _senderEmail;
    private readonly string _senderName;
    private readonly string _appPassword;

    // Manifest resource name: <DefaultNamespace>.<FolderPath>.<FileName>
    private const string TemplateResourceName = "Business.Templates.EmailVerificationCode.html";

    /// <summary>
    /// Reads SMTP credentials from the <c>Email:*</c> configuration section.
    /// </summary>
    /// <param name="configuration">Application configuration.</param>
    /// <exception cref="InvalidOperationException">Thrown when a required SMTP key is missing.</exception>
    public EmailService(IConfiguration configuration)
    {
        _smtpHost = configuration["Email:SmtpHost"]
            ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");
        _smtpPort = int.Parse(configuration["Email:SmtpPort"] ?? "587");
        _senderEmail = configuration["Email:SenderEmail"]
            ?? throw new InvalidOperationException("Email:SenderEmail is not configured.");
        _senderName = configuration["Email:SenderName"] ?? "EduChatAI";
        _appPassword = configuration["Email:AppPassword"]
            ?? throw new InvalidOperationException("Email:AppPassword is not configured.");
    }

    /// <summary>
    /// Builds a MimeMessage from the embedded HTML template and sends it via Gmail SMTP with STARTTLS.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="toName">The recipient's display name.</param>
    /// <param name="code">The 6-digit OTP substituted into the <c>{{CODE}}</c> placeholder.</param>
    public async Task SendVerificationCodeAsync(string toEmail, string toName, string code)
    {
        var htmlBody = await BuildHtmlBodyAsync(code);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_senderName, _senderEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = "EduChatAI – Mã xác nhận của bạn";
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_smtpHost, _smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_senderEmail, _appPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(quit: true);
    }

    // ── Helpers ──────────────────────────────────────────────────

    /// <summary>
    /// Reads the HTML template from the embedded assembly resource and substitutes {{CODE}}.
    /// </summary>
    private static async Task<string> BuildHtmlBodyAsync(string code)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(TemplateResourceName)
            ?? throw new InvalidOperationException(
                $"Embedded email template '{TemplateResourceName}' not found. " +
                $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        var template = await reader.ReadToEndAsync();
        return template.Replace("{{CODE}}", code);
    }
}
