using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Home;

/// <summary>
/// Displays the privacy policy page.
/// </summary>
[Authorize]
public class PrivacyModel : PageModel
{
    /// <summary>
    /// Loads the privacy policy page.
    /// </summary>
    public void OnGet()
    {
    }
}
