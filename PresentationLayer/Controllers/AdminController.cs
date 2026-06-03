using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

/// <summary>
/// Handles all admin-panel pages and AJAX endpoints for user management.
/// All actions are restricted to the <c>Admin</c> role.
/// </summary>
[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController(
    IUserManagementService userManagementService,
    UserManager<ApplicationUser> userManager) : Controller
{
    private readonly IUserManagementService _userMgmt = userManagementService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    // ── USER MANAGE PAGE ──────────────────────────────────────────

    /// <summary>
    /// Displays the user management page with a paginated, filtered list of users.
    /// </summary>
    /// <param name="name">Optional name filter.</param>
    /// <param name="email">Optional email filter.</param>
    /// <param name="role">Optional role filter.</param>
    /// <param name="limit">Page size (default 10).</param>
    /// <param name="offset">Record offset (default 0).</param>
    [HttpGet("user-manage")]
    public async Task<IActionResult> UserManage(
        string? name, string? email, string? role, int limit = 10, int offset = 0)
    {
        var users = await _userMgmt.GetPagedUsersAsync(name, email, role, limit, offset);
        var roles = await _userMgmt.GetAllRolesAsync();

        var vm = new AdminUserListVm
        {
            NameFilter = name,
            EmailFilter = email,
            RoleFilter = role,
            Limit = limit,
            Offset = offset,
            Users = users,
            AvailableRoles = roles,
        };
        return View(vm);
    }

    // ── ROLES API ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the list of available roles as JSON for populating the role dropdown.
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _userMgmt.GetAllRolesAsync();
        return Json(roles);
    }

    // ── CREATE ────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new user account and triggers the email-verification flow.
    /// Returns JSON so the page can show inline success/error without a full reload.
    /// </summary>
    /// <param name="vm">Create-user form data submitted via AJAX.</param>
    [HttpPost("users/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid form data." });

        var (success, error) = await _userMgmt.CreateUserAsync(
            new Domain.Contracts.CreateUserDto(vm.FullName, vm.Email, vm.Password, vm.Role));

        return Json(new { success, error });
    }

    // ── UPDATE ────────────────────────────────────────────────────

    /// <summary>
    /// Updates a user's profile (name, email, role). Applies optimistic-concurrency protection
    /// using the <c>updatedAt</c> token sent from the client.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="vm">Update form data.</param>
    [HttpPut("users/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid form data." });

        if (id != vm.UserId)
            return BadRequest(new { success = false, error = "User ID mismatch." });

        var (success, error) = await _userMgmt.UpdateUserAsync(
            new Domain.Contracts.UpdateUserDto(vm.UserId, vm.FullName, vm.Email, vm.Role, vm.UpdatedAt));

        if (success)
        {
            var message = vm.Email != vm.OriginalEmail
                ? "Changes saved! A verification email has been sent to the new email address."
                : "Changes saved successfully!";
            return Json(new { success, message });
        }

        return Json(new { success, error });
    }

    // ── SOFT DELETE ───────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes a user account. Sends a deletion-notification email.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    [HttpDelete("users/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var (success, error) = await _userMgmt.SoftDeleteUserAsync(id);
        return Json(new { success, error });
    }

    // ── DISABLE ───────────────────────────────────────────────────

    /// <summary>
    /// Disables a user account using optimistic-concurrency protection.
    /// Sends a disable-notification email on success.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="body">Body containing the <c>updatedAt</c> concurrency token.</param>
    [HttpPost("users/{id:guid}/disable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableUser(Guid id, [FromBody] ConcurrencyTokenBody body)
    {
        var (success, error) = await _userMgmt.DisableUserAsync(id, body.UpdatedAt);
        return Json(new { success, error });
    }

    // ── REACTIVATE ────────────────────────────────────────────────

    /// <summary>
    /// Re-enables a previously disabled user account using optimistic-concurrency protection.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="body">Body containing the <c>updatedAt</c> concurrency token.</param>
    [HttpPost("users/{id:guid}/reactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateUser(Guid id, [FromBody] ConcurrencyTokenBody body)
    {
        var (success, error) = await _userMgmt.ReactivateUserAsync(id, body.UpdatedAt);
        return Json(new { success, error });
    }
}

/// <summary>Minimal request body carrying only the optimistic-concurrency token.</summary>
public record ConcurrencyTokenBody(DateTimeOffset UpdatedAt);
