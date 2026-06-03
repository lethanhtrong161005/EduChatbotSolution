using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

/// <summary>
/// Represents an application user with extended profile, lifecycle, and audit fields.
/// Extends ASP.NET Identity's <see cref="IdentityUser{TKey}"/> with soft-delete
/// and optimistic-concurrency support for the admin user management feature.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>Gets or sets the user's full display name.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the account is active.
    /// Disabled accounts cannot authenticate.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the UTC timestamp of the last record update.
    /// Used as an optimistic-concurrency token for admin Update and Disable operations
    /// to prevent two admins from overwriting each other's changes.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the UTC timestamp when the account was soft-deleted.
    /// A <c>null</c> value means the account is not deleted.
    /// Soft-deleted accounts are hidden from normal operations but remain in the database.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the conversations belonging to this user.</summary>
    public virtual ICollection<ChatSession> ChatSessions { get; set; } = [];
    public virtual ICollection<SubjectMembership> SubjectMemberships { get; set; } = [];
}
