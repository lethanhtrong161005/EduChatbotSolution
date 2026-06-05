using Domain.Common;

namespace Domain.Contracts;

/// <summary>
/// Defines the contract for admin-level user management operations including
/// paginated listing, creation, update, soft-delete, disable, and reactivation.
/// All mutating operations that are susceptible to concurrent admin edits accept
/// an <c>updatedAt</c> timestamp for optimistic-concurrency checking.
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Returns a paginated list of users filtered by optional name, email, and role criteria.
    /// Soft-deleted accounts are included (shown as deleted in the UI).
    /// </summary>
    /// <param name="nameFilter">Optional substring match on <see cref="Domain.Entities.ApplicationUser.FullName"/>.</param>
    /// <param name="emailFilter">Optional substring match on email address.</param>
    /// <param name="roleFilter">Optional exact role name filter.</param>
    /// <param name="limit">Maximum number of records to return (page size).</param>
    /// <param name="offset">Number of records to skip (zero-based).</param>
    /// <returns>A paginated list of <see cref="UserManagementItemDto"/> records.</returns>
    Task<PaginatedList<UserManagementItemDto>> GetPagedUsersAsync(
        string? nameFilter,
        string? emailFilter,
        string? roleFilter,
        int limit,
        int offset);

    /// <summary>
    /// Returns all available roles from the database for populating the role dropdown UI.
    /// </summary>
    /// <returns>A list of role names.</returns>
    Task<IList<string>> GetAllRolesAsync();

    /// <summary>
    /// Creates a new active, email-confirmed user account with the specified role,
    /// then emails the initial login credentials to the user.
    /// </summary>
    /// <param name="dto">The user creation data including full name, email, password, and role.</param>
    /// <returns>A success/error tuple. <c>Error</c> is null on success.</returns>
    Task<(bool Success, string? Error)> CreateUserAsync(CreateUserDto dto);

    /// <summary>
    /// Updates an existing user's profile (name, email, and/or role).
    /// Uses the <paramref name="dto.UpdatedAt"/> field for optimistic-concurrency checking.
    /// If the email changes, a new email-verification flow is triggered.
    /// </summary>
    /// <param name="dto">The update data including the current <c>UpdatedAt</c> timestamp.</param>
    /// <returns>A success/error tuple. Returns a conflict error if <c>UpdatedAt</c> mismatches.</returns>
    Task<(bool Success, string? Error)> UpdateUserAsync(UpdateUserDto dto);

    /// <summary>
    /// Soft-deletes a user by setting <c>DeletedAt</c> to the current UTC time.
    /// The record is retained in the database. Sends a deletion-notification email to the user.
    /// Disabled accounts can also be soft-deleted.
    /// </summary>
    /// <param name="userId">The ID of the user to soft-delete.</param>
    /// <returns>A success/error tuple.</returns>
    Task<(bool Success, string? Error)> SoftDeleteUserAsync(Guid userId);

    /// <summary>
    /// Disables a user account, preventing authentication.
    /// Sends a disable-notification email. Uses <paramref name="updatedAt"/>
    /// for optimistic-concurrency protection.
    /// </summary>
    /// <param name="userId">The ID of the user to disable.</param>
    /// <param name="updatedAt">The <c>UpdatedAt</c> timestamp from the client for concurrency checking.</param>
    /// <returns>A success/error tuple. Returns a conflict error if <c>UpdatedAt</c> mismatches.</returns>
    Task<(bool Success, string? Error)> DisableUserAsync(Guid userId, DateTimeOffset updatedAt);

    /// <summary>
    /// Re-enables a previously disabled user account.
    /// Uses <paramref name="updatedAt"/> for optimistic-concurrency protection.
    /// </summary>
    /// <param name="userId">The ID of the user to reactivate.</param>
    /// <param name="updatedAt">The <c>UpdatedAt</c> timestamp from the client for concurrency checking.</param>
    /// <returns>A success/error tuple. Returns a conflict error if <c>UpdatedAt</c> mismatches.</returns>
    Task<(bool Success, string? Error)> ReactivateUserAsync(Guid userId, DateTimeOffset updatedAt);
}

/// <summary>Represents a single user record for display in the admin user management list.</summary>
public record UserManagementItemDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    bool IsActive,
    bool IsDeleted,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt
);

/// <summary>Data transfer object for admin user creation.</summary>
public record CreateUserDto(
    string FullName,
    string Email,
    string Password,
    string Role
);

/// <summary>Data transfer object for admin user update with optimistic-concurrency token.</summary>
public record UpdateUserDto(
    Guid UserId,
    string FullName,
    string Email,
    string Role,
    DateTimeOffset UpdatedAt
);
