using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Home;

/// <summary>
/// Displays the authenticated user's dashboard with user profile information.
/// </summary>
[Authorize]
public class IndexModel(UserManager<ApplicationUser> userManager) : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    /// <summary>Gets the user's full name to display.</summary>
    public string FullName { get; private set; } = "User";

    /// <summary>Gets the user's email address to display.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets the user's primary role to display.</summary>
    public string Role { get; private set; } = "Student";

    /// <summary>
    /// Loads the dashboard data.
    /// </summary>
    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            FullName = user.FullName;
            Email = user.Email ?? string.Empty;

            var roles = await _userManager.GetRolesAsync(user);
            Role = roles.FirstOrDefault() ?? "Student";
        }
        return Page();
    }
}
