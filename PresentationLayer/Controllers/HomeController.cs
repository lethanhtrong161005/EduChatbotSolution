using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Models;

namespace PresentationLayer.Controllers;

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

    /// <summary>Displays the error page.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
