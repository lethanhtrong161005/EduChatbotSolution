using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Entities;

/// <summary>
/// Represents an application user, mapped to the <c>users</c> table.
/// Uses a custom schema (not ASP.NET Identity) aligned with the project database script.
/// </summary>
[Index(nameof(Email), IsUnique = true)]
public class ApplicationUser
{
    /// <summary>Gets or sets the unique user identifier (UUID).</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the user's full display name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the user's unique email address.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the BCrypt-hashed password.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the user's email has been verified.</summary>
    public bool IsEmailVerified { get; set; }

    /// <summary>Gets or sets whether the user account is active.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the UTC timestamp when the account was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the last account update.</summary>
    public DateTime? UpdatedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the roles assigned to this user.</summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];

    /// <summary>Gets or sets the conversations belonging to this user.</summary>
    public virtual ICollection<Conversation> Conversations { get; set; } = [];
}
