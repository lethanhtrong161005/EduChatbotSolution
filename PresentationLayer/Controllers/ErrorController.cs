using Microsoft.AspNetCore.Mvc;
using Presentation.Defaults;
using Presentation.Models;
using System.Diagnostics;

namespace Presentation.Controllers;

public class ErrorController : Controller
{
    /// <summary>Displays the error page.</summary>
    [HttpGet(ErrorHandlingDefaults.ErrorPagePath)]
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
