using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Defaults;
using Presentation.Models;
using System.Diagnostics;

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

    /// <summary>Displays the error page.</summary>
    [HttpGet("/error")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var problemDetails = HttpContext.Items[ErrorHandlingDefaults.ProblemDetailsHttpContextItemName] as ProblemDetails;

        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Title = problemDetails?.Title,
            Detail = problemDetails?.Detail,
        });
    }
}
