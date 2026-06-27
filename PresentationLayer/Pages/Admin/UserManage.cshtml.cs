using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.ViewModels;

namespace Presentation.Pages.Admin;

/// <summary>
/// Displays the administrator user management page with filters and pagination.
/// Handles page display and AJAX user management endpoints.
/// </summary>
[Authorize(Roles = "Admin")]
public class UserManageModel(
    IUserManagementService userManagementService,
    IResourceRealtimeNotifier notifier)
    : PageModel
{
    private readonly IUserManagementService _userManagementService = userManagementService;
    private readonly IResourceRealtimeNotifier _notifier = notifier;

    /// <summary>
    /// Gets the user management view model rendered by the page.
    /// </summary>
    public AdminUserListVm ViewModel { get; private set; } = new();

    [FromHeader]
    public string CallerConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Loads users and roles for the user management page.
    /// </summary>
    /// <param name="name">Optional name filter.</param>
    /// <param name="email">Optional email filter.</param>
    /// <param name="role">Optional role filter.</param>
    /// <param name="limit">The requested page size.</param>
    /// <param name="offset">The zero-based record offset.</param>
    /// <returns>A task that renders the page.</returns>
    public async Task OnGetAsync(string? name, string? email, string? role, int limit = 10, int offset = 0)
    {
        var users = await _userManagementService.GetPagedUsersAsync(name, email, role, limit, offset);
        var roles = await _userManagementService.GetAllRolesAsync();

        ViewModel = new AdminUserListVm
        {
            NameFilter = name,
            EmailFilter = email,
            RoleFilter = role,
            Limit = limit,
            Offset = offset,
            Users = users,
            AvailableRoles = roles,
        };
    }

    public async Task<IActionResult> OnGetGetUsersAsync(string? name, string? email, string? role, int limit = 10, int offset = 0)
    {
        var users = await _userManagementService.GetPagedUsersAsync(name, email, role, limit, offset);

        var dto = new
        {
            Users = users,
            TotalCount = users.Count,
        };

        return new JsonResult(dto);
    }

    /// <summary>
    /// Creates a new active user account and emails the initial login credentials.
    /// Returns JSON so the page can show inline success/error without a full reload.
    /// </summary>
    /// <param name="vm">Create-user form data submitted via AJAX.</param>
    public async Task<IActionResult> OnPostCreateUserAsync([FromBody] AdminCreateUserVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid form data." });

        var (success, user, error) = await _userManagementService.CreateUserAsync(
            new CreateUserDto(vm.FullName, vm.Email, vm.Password, vm.Role));

        if (!success)
            return StatusCode(500, new { success, error });

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Created,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, error });
    }

    /// <summary>
    /// Updates a user's profile (name, email, role). Applies optimistic-concurrency protection.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="vm">Update form data.</param>
    public async Task<IActionResult> OnPutUpdateUserAsync([FromQuery] Guid id, [FromBody] AdminUpdateUserVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid form data." });

        if (id != vm.UserId)
            return BadRequest(new { success = false, error = "User ID mismatch." });

        var (success, user, error) = await _userManagementService.UpdateUserAsync(
            new UpdateUserDto(vm.UserId, vm.FullName, vm.Email, vm.Role, vm.UpdatedAt));

        if (!success)
            return StatusCode(500, new { success, error });

        var message = vm.Email != vm.OriginalEmail
            ? "Changes saved! A verification email has been sent to the new email address."
            : "Changes saved successfully!";

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Updated,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, message });
    }

    /// <summary>
    /// Soft-deletes a user account. Sends a deletion-notification email.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    public async Task<IActionResult> OnDeleteDeleteUserAsync([FromQuery] Guid id)
    {
        var (success, user, error) = await _userManagementService.SoftDeleteUserAsync(id);

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Deleted,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, error });
    }

    /// <summary>
    /// Disables a user account using optimistic-concurrency protection.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="body">Body containing the concurrency token.</param>
    public async Task<IActionResult> OnPutDisableUserAsync([FromQuery] Guid id, [FromBody] ConcurrencyTokenBody body)
    {
        var (success, user, error) = await _userManagementService.DisableUserAsync(id, body.UpdatedAt);

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Disabled,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, error });
    }

    /// <summary>
    /// Re-enables a previously disabled user account using optimistic-concurrency protection.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="body">Body containing the concurrency token.</param>
    public async Task<IActionResult> OnPutReactivateUserAsync([FromQuery] Guid id, [FromBody] ConcurrencyTokenBody body)
    {
        var (success, user, error) = await _userManagementService.ReactivateUserAsync(id, body.UpdatedAt);

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.User,
            Action = ResourceAction.Enabled,
            ResourceId = user!.Id.ToString(),
            ResourceName = user.FullName,
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return new JsonResult(new { success, error });
    }
}
