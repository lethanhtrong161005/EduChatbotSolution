using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers;

/// <summary>
/// Handles the main dashboard pages of the application.
/// All actions require an authenticated user.
/// </summary>
[Authorize]
public class HomeController : Controller
{
    /// <summary>Displays the main dashboard page after login.</summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>Displays the privacy policy page.</summary>
    public IActionResult Privacy()
    {
        return View();
    }
}
