using System;
using System.Collections.Generic;

namespace DataAccessLayer.Entities;

/// <summary>
/// Represents a user account in the EduChatAI system.
/// Contains authentication credentials and profile information for system users.
/// </summary>
public class User
{
    /// <summary>
    /// The unique identifier of the user (UUID format).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The full name of the user.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the user (unique across the system).
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The hashed password for secure storage and authentication.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the user's email address has been verified.
    /// </summary>
    public bool IsEmailVerified { get; set; } = false;

    /// <summary>
    /// Indicates whether the user account is active and can access the system.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The date and time when the user account was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The date and time when the user account was last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property for the roles assigned to this user.
    /// </summary>
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
