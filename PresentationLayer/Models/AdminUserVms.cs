using Domain.Contracts;
using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

/// <summary>
/// View-model for the admin user management list page.
/// Carries filter parameters and the paginated result set.
/// </summary>
public class AdminUserListVm
{
    /// <summary>Gets or sets the name substring filter.</summary>
    public string? NameFilter { get; set; }

    /// <summary>Gets or sets the email substring filter.</summary>
    public string? EmailFilter { get; set; }

    /// <summary>Gets or sets the exact role name filter.</summary>
    public string? RoleFilter { get; set; }

    /// <summary>Gets or sets the page size (number of records per page).</summary>
    public int Limit { get; set; } = 10;

    /// <summary>Gets or sets the zero-based record offset.</summary>
    public int Offset { get; set; } = 0;

    /// <summary>Gets or sets the paginated user list.</summary>
    public Domain.Common.PaginatedList<UserManagementItemDto> Users { get; set; } = [];

    /// <summary>Gets or sets all available roles for the filter dropdown.</summary>
    public IList<string> AvailableRoles { get; set; } = [];
}

/// <summary>
/// Input model for creating a new user via the admin modal.
/// </summary>
public class AdminCreateUserVm
{
    /// <summary>Gets or sets the full display name.</summary>
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address.</summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the initial password set by the admin.</summary>
    [Required(ErrorMessage = "Password is required.")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    /// <summary>Gets or sets the role name to assign.</summary>
    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// Input model for updating an existing user via the admin modal.
/// Includes <see cref="UpdatedAt"/> as the optimistic-concurrency token.
/// </summary>
public class AdminUpdateUserVm
{
    /// <summary>Gets or sets the user's database ID.</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Gets or sets the updated full display name.</summary>
    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the updated email address.</summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = string.Empty;

    /// <summary>Gets or sets the updated role name.</summary>
    [Required(ErrorMessage = "Role is required.")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optimistic-concurrency token.
    /// Must match the current <c>UpdatedAt</c> value in the database.
    /// </summary>
    [Required]
    public DateTimeOffset UpdatedAt { get; set; }
}
