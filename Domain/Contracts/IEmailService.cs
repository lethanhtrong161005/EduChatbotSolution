namespace Domain.Contracts;

/// <summary>
/// Defines the contract for sending transactional emails via SMTP.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a 6-digit verification code to the specified recipient using the branded HTML template.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="toName">The recipient's display name.</param>
    /// <param name="code">The 6-digit verification code to embed in the email body.</param>
    /// <exception cref="InvalidOperationException">Thrown when SMTP configuration is missing or the send operation fails.</exception>
    Task SendVerificationCodeAsync(string toEmail, string toName, string code);
}
