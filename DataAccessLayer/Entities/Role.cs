namespace DataAccessLayer.Entities;

/// <summary>
/// Represents a role, mapped to the <c>roles</c> table.
/// Uses SERIAL (integer) primary key as defined in the database script.
/// </summary>
public class Role
{
    /// <summary>Gets or sets the auto-incremented role identifier.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the unique role name (e.g., Admin, Lecturer, Student).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description of the role's purpose.</summary>
    public string? Description { get; set; }

    // ── Navigation ──────────────────────────────────────────
    /// <summary>Gets or sets the user-role assignments for this role.</summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
}
