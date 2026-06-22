using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.ViewModels;

namespace Presentation.Pages.Admin;

/// <summary>
/// Displays the administrator user management page with filters and pagination.
/// </summary>
[Authorize(Roles = "Admin")]
public class UserManageModel(IUserManagementService userManagementService) : PageModel
{
    private readonly IUserManagementService _userManagementService = userManagementService;

    /// <summary>
    /// Gets the user management view model rendered by the page.
    /// </summary>
    public AdminUserListVm ViewModel { get; private set; } = new();

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
}
