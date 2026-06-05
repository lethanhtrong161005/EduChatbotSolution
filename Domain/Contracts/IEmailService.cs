namespace Domain.Contracts;

/// <summary>
/// Defines the contract for sending transactional emails via SMTP.
/// Covers OTP verification, admin-created account credential delivery, and lifecycle notifications.
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

    /// <summary>
    /// Sends login credentials for a user account created directly by an administrator.
    /// Admin-created accounts are already email-confirmed, so no OTP is included.
    /// </summary>
    /// <param name="toEmail">The new account's email address.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="plainPassword">The plain-text password set by the admin.</param>
    Task SendAdminCreatedCredentialsAsync(string toEmail, string toName, string plainPassword);

    /// <summary>
    /// Sends a password-reset verification code to the account owner.
    /// The code must be verified before a new password is saved.
    /// </summary>
    /// <param name="toEmail">The account email address.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="code">The 6-digit OTP code to embed in the email.</param>
    Task SendPasswordResetCodeAsync(string toEmail, string toName, string code);

    /// <summary>
    /// Sends a verification email when an admin updates a user's email to a new address.
    /// The user must verify the new email before it takes effect.
    /// </summary>
    /// <param name="toEmail">The new email address being verified.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="code">The 6-digit OTP code to embed in the email.</param>
    Task SendEmailUpdateVerifyAsync(string toEmail, string toName, string code);

    /// <summary>
    /// Notifies a user that their account has been disabled by an administrator.
    /// Includes contact information for support.
    /// </summary>
    /// <param name="toEmail">The user's current email address.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="contactEmail">The support/contact email address to include in the email.</param>
    Task SendAccountDisabledAsync(string toEmail, string toName, string contactEmail);

    /// <summary>
    /// Notifies a user that their account has been deleted (soft-deleted) by an administrator.
    /// Includes contact information for any appeals or data-access requests.
    /// </summary>
    /// <param name="toEmail">The user's email address at the time of deletion.</param>
    /// <param name="toName">The user's full name.</param>
    /// <param name="contactEmail">The support/contact email address to include in the email.</param>
    Task SendAccountDeletedAsync(string toEmail, string toName, string contactEmail);
}
