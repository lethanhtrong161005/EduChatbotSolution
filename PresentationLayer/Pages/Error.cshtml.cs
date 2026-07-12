using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Constants;
using Presentation.ViewModels;
using System.Diagnostics;

namespace Presentation.Pages;

/// <summary>
/// Displays problem details captured by the custom exception middleware.
/// </summary>
public class ErrorModel : PageModel
{
    /// <summary>
    /// Gets the error view model rendered by the page.
    /// </summary>
    public ErrorVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Loads error details from the current HTTP context.
    /// </summary>
    public IActionResult OnGet()
    {
        var problemDetails = HttpContext.Items[ErrorHandlingConstants.ProblemDetailsHttpContextItemName] as ProblemDetails;

        ViewModel = new ErrorVm
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Title = problemDetails?.Title,
            Detail = problemDetails?.Detail,
        };

        var status = problemDetails?.Status ?? 500;

        if (status == StatusCodes.Status404NotFound)
        {
            return RedirectToPage("/NotFound");
        }
        else if (status == StatusCodes.Status401Unauthorized || status == StatusCodes.Status403Forbidden)
        {
            TempData["GlobalError"] = problemDetails?.Title ?? "Access Denied. You do not have permission to view this page.";
            return RedirectToPage("/Home/Index");
        }
        else if (status != 500)
        {
            // For other handled errors (like 400 Bad Request, 422 Unprocessable Entity), we might want to just show a toast
            TempData["GlobalError"] = problemDetails?.Title ?? "An error occurred.";
            return RedirectToPage("/Home/Index");
        }

        // For actual 500 Internal Server errors, we still show the error page (or you can redirect)
        return Page();
    }
}
