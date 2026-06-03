using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Business.Services;

/// <summary>
/// Implements admin-level user management operations: paginated listing, creation,
/// update, soft-delete, disable, and reactivation. Optimistic-concurrency protection
/// is applied to all mutating operations via the <c>UpdatedAt</c> timestamp column.
/// </summary>
public class UserManagementService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IEmailService emailService,
    IEmailVerificationService emailVerificationService,
    IConfiguration configuration) : IUserManagementService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager = roleManager;
    private readonly IEmailService _emailService = emailService;
    private readonly IEmailVerificationService _emailVerificationService = emailVerificationService;
    private readonly string _contactEmail = configuration["Email:SenderEmail"] ?? "support@educhatai.com";

    // ── READ ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns a paginated list of all users filtered by optional name, email, and role.
    /// Soft-deleted accounts are included and flagged with <c>IsDeleted = true</c>.
    /// </summary>
    /// <param name="nameFilter">Optional substring match on full name (case-insensitive).</param>
    /// <param name="emailFilter">Optional substring match on email (case-insensitive).</param>
    /// <param name="roleFilter">Optional exact role name match.</param>
    /// <param name="limit">Page size (max records per page).</param>
    /// <param name="offset">Number of records to skip.</param>
    /// <returns>A <see cref="PaginatedList{T}"/> of <see cref="UserManagementItemDto"/>.</returns>
    public async Task<PaginatedList<UserManagementItemDto>> GetPagedUsersAsync(
        string? nameFilter,
        string? emailFilter,
        string? roleFilter,
        int limit,
        int offset)
    {
        // 1. Build base query — order by creation date descending
        var query = _userManager.Users.AsNoTracking();

        // 2. Apply filters
        if (!string.IsNullOrWhiteSpace(nameFilter))
            query = query.Where(u => u.FullName.ToLower().Contains(nameFilter.ToLower()));

        if (!string.IsNullOrWhiteSpace(emailFilter))
            query = query.Where(u => u.Email != null && u.Email.ToLower().Contains(emailFilter.ToLower()));

        // 3. Count before pagination
        var totalCount = await query.CountAsync();

        // 4. Paginate
        var users = await query
            .OrderByDescending(u => u.UpdatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        // 5. Load roles for each user (Identity doesn't support JOIN in EF for roles)
        var dtos = new List<UserManagementItemDto>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "Unknown";

            // Apply role filter after role lookup
            if (!string.IsNullOrWhiteSpace(roleFilter) &&
                !string.Equals(primaryRole, roleFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            dtos.Add(new UserManagementItemDto(
                user.Id,
                user.FullName,
                user.Email ?? string.Empty,
                primaryRole,
                user.IsActive,
                user.DeletedAt.HasValue,
                user.UpdatedAt,
                user.DeletedAt
            ));
        }

        // Recalculate total for role-filtered scenarios
        var effectiveTotal = string.IsNullOrWhiteSpace(roleFilter) ? totalCount : dtos.Count;

        return new PaginatedList<UserManagementItemDto>(
            dtos, effectiveTotal, limit > 0 ? limit : 10, (offset / (limit > 0 ? limit : 10)) + 1);
    }

    /// <summary>
    /// Returns all role names from the database for the role selector dropdown.
    /// </summary>
    public async Task<IList<string>> GetAllRolesAsync()
    {
        return await _roleManager.Roles
            .AsNoTracking()
            .Select(r => r.Name!)
            .ToListAsync();
    }

    // ── CREATE ────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new user in the database with the given role and initiates email verification.
    /// User is saved immediately with EmailConfirmed = false; account activation happens after
    /// the user verifies their email via OTP.
    /// </summary>
    /// <param name="dto">Creation data: full name, email, password, and role.</param>
    /// <returns>Success/error tuple. <c>Error</c> is null on success.</returns>
    /// <exception cref="ArgumentNullException">Thrown when required fields are missing.</exception>
    public async Task<(bool Success, string? Error)> CreateUserAsync(CreateUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // 1. Check email uniqueness
        if (await _userManager.FindByEmailAsync(dto.Email) is not null)
            return (false, "An account with this email address already exists.");

        // 2. Validate role exists in DB
        if (!await _roleManager.RoleExistsAsync(dto.Role))
            return (false, $"Role '{dto.Role}' does not exist.");

        // 3. Create user in database immediately with EmailConfirmed = false
        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            EmailConfirmed = false,
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        };

        var createResult = await _userManager.CreateAsync(user);
        if (!createResult.Succeeded)
            return (false, createResult.Errors.FirstOrDefault()?.Description ?? "Failed to create user.");

        // 4. Assign role
        var roleResult = await _userManager.AddToRoleAsync(user, dto.Role);
        if (!roleResult.Succeeded)
            return (false, roleResult.Errors.FirstOrDefault()?.Description ?? "Failed to assign role.");

        // 5. Add identity claims
        await _userManager.AddClaimsAsync(user,
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, dto.Role),
        ]);

        // 6. Initiate email verification
        var (emailSuccess, emailError) = await _emailVerificationService.InitiateEmailVerificationForExistingUserAsync(dto.Email, dto.FullName);
        if (!emailSuccess)
        {
            // Email send failed — delete the user we just created
            await _userManager.DeleteAsync(user);
            return (false, emailError ?? "Failed to send verification email.");
        }

        return (true, null);
    }

    // ── UPDATE ────────────────────────────────────────────────────

    /// <summary>
    /// Updates a user's profile. Applies optimistic-concurrency check against <c>UpdatedAt</c>.
    /// If the email changes, triggers an email-update verification flow for the new address.
    /// </summary>
    /// <param name="dto">Update data with the current <c>UpdatedAt</c> timestamp from the UI.</param>
    /// <returns>Success/error tuple. Returns a 409-style error on concurrency conflict.</returns>
    public async Task<(bool Success, string? Error)> UpdateUserAsync(UpdateUserDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        // 1. Load user
        var user = await _userManager.FindByIdAsync(dto.UserId.ToString());
        if (user is null)
            return (false, "User not found.");

        // 2. Optimistic concurrency check
        if (user.UpdatedAt != dto.UpdatedAt)
            return (false, "This record was modified by another administrator. Please refresh and try again.");

        // 3. Detect email change
        var emailChanged = !string.Equals(user.Email, dto.Email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            // Check new email not already in use
            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing is not null && existing.Id != user.Id)
                return (false, "The new email address is already in use by another account.");

            // Initiate email-update verification (stores userId + newEmail in Redis)
            var (ok, err) = await _emailVerificationService
                .InitiateEmailUpdateVerificationAsync(dto.Email, dto.FullName, user.Id);
            if (!ok)
                return (false, err);
        }

        // 4. Update non-email fields
        user.FullName = dto.FullName;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // 5. Update role
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(dto.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, dto.Role);

            // Sync role claim
            var roleClaims = (await _userManager.GetClaimsAsync(user))
                .Where(c => c.Type == ClaimTypes.Role)
                .ToList();
            await _userManager.RemoveClaimsAsync(user, roleClaims);
            await _userManager.AddClaimAsync(user, new Claim(ClaimTypes.Role, dto.Role));
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, result.Errors.FirstOrDefault()?.Description ?? "Failed to update user.");

        return (true, null);
    }

    // ── SOFT DELETE ───────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes a user by setting <c>DeletedAt</c>. Sends a deletion notification email.
    /// Disabled accounts are also eligible for soft-deletion.
    /// </summary>
    /// <param name="userId">The user's database ID.</param>
    /// <returns>Success/error tuple.</returns>
    public async Task<(bool Success, string? Error)> SoftDeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, "User not found.");

        if (user.DeletedAt.HasValue)
            return (false, "This account has already been deleted.");

        // 1. Soft-delete
        user.DeletedAt = DateTimeOffset.UtcNow;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, result.Errors.FirstOrDefault()?.Description ?? "Failed to delete user.");

        // 2. Send notification email (fire-and-forget on failure — do not block deletion)
        try
        {
            await _emailService.SendAccountDeletedAsync(
                user.Email ?? string.Empty, user.FullName, _contactEmail);
        }
        catch
        {
            // Email failure is non-critical; deletion is already committed
        }

        return (true, null);
    }

    // ── DISABLE ───────────────────────────────────────────────────

    /// <summary>
    /// Disables a user account. Applies optimistic-concurrency check.
    /// Sends a disable-notification email to the user.
    /// </summary>
    /// <param name="userId">The user's database ID.</param>
    /// <param name="updatedAt">The <c>UpdatedAt</c> timestamp from the client for concurrency checking.</param>
    /// <returns>Success/error tuple.</returns>
    public async Task<(bool Success, string? Error)> DisableUserAsync(Guid userId, DateTimeOffset updatedAt)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, "User not found.");

        // 1. Optimistic concurrency check
        if (user.UpdatedAt != updatedAt)
            return (false, "This record was modified by another administrator. Please refresh and try again.");

        if (!user.IsActive)
            return (false, "The account is already disabled.");

        // 2. Disable
        user.IsActive = false;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, result.Errors.FirstOrDefault()?.Description ?? "Failed to disable account.");

        // 3. Send notification email
        try
        {
            await _emailService.SendAccountDisabledAsync(
                user.Email ?? string.Empty, user.FullName, _contactEmail);
        }
        catch
        {
            // Email failure is non-critical
        }

        return (true, null);
    }

    // ── REACTIVATE ────────────────────────────────────────────────

    /// <summary>
    /// Re-enables a disabled user account. Applies optimistic-concurrency check.
    /// </summary>
    /// <param name="userId">The user's database ID.</param>
    /// <param name="updatedAt">The <c>UpdatedAt</c> timestamp from the client for concurrency checking.</param>
    /// <returns>Success/error tuple.</returns>
    public async Task<(bool Success, string? Error)> ReactivateUserAsync(Guid userId, DateTimeOffset updatedAt)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return (false, "User not found.");

        // 1. Optimistic concurrency check
        if (user.UpdatedAt != updatedAt)
            return (false, "This record was modified by another administrator. Please refresh and try again.");

        if (user.IsActive)
            return (false, "The account is already active.");

        if (user.DeletedAt.HasValue)
            return (false, "Cannot reactivate a deleted account.");

        // 2. Reactivate
        user.IsActive = true;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return (false, result.Errors.FirstOrDefault()?.Description ?? "Failed to reactivate account.");

        return (true, null);
    }
}
