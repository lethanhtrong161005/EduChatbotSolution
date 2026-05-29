using System;
using System.Collections.Generic;

namespace DataAccessLayer.Entities;

/// <summary>
/// Represents a system role that defines permissions and responsibilities for users.
/// Roles are assigned to users through the UserRole junction table.
/// </summary>
public class Role
{
    /// <summary>
    /// The unique identifier of the role.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The name of the role (e.g., Admin, Lecturer, Student).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// A brief description of the role's purpose and responsibilities.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property for users assigned to this role.
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
