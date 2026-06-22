using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Home;

/// <summary>
/// Displays the authenticated user's dashboard.
/// </summary>
[Authorize]
public class IndexModel : PageModel
{
    /// <summary>
    /// Loads the dashboard page for the current user.
    /// </summary>
    public void OnGet()
    {
    }
}
