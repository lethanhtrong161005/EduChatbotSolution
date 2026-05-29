using System;

namespace DataAccessLayer.Entities;

/// <summary>
/// Represents the many-to-many relationship between users and roles.
/// Allows users to have multiple roles and roles to be assigned to multiple users.
/// </summary>
public class UserRole
{
    /// <summary>
    /// The unique identifier of the user in this role assignment.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The unique identifier of the role being assigned.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Navigation property for the user.
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Navigation property for the role.
    /// </summary>
    public virtual Role Role { get; set; } = null!;
}
