namespace DataAccessLayer.Entities;

/// <summary>
/// Represents the many-to-many join between users and roles,
/// mapped to the <c>user_roles</c> table.
/// </summary>
public class UserRole
{
    /// <summary>Gets or sets the foreign key to the <see cref="ApplicationUser"/>.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the foreign key to the <see cref="Role"/>.</summary>
    public int RoleId { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the associated user.</summary>
    public virtual ApplicationUser User { get; set; } = null!;

    /// <summary>Gets or sets the associated role.</summary>
    public virtual Role Role { get; set; } = null!;
}
