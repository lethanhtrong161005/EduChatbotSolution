using Microsoft.AspNetCore.Mvc;
using Presentation.Constants;
using Presentation.ViewModels;
using System.Diagnostics;

namespace Presentation.Controllers;

public class ErrorController : Controller
{
    /// <summary>Displays the error page.</summary>
    [HttpGet(ErrorHandlingConstants.ErrorPagePath)]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var problemDetails = HttpContext.Items[ErrorHandlingConstants.ProblemDetailsHttpContextItemName] as ProblemDetails;

        return View(new ErrorVm
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Title = problemDetails?.Title,
            Detail = problemDetails?.Detail,
        });
    }
}
