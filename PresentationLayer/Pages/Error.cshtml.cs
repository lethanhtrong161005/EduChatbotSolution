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
    public void OnGet()
    {
        var problemDetails = HttpContext.Items[ErrorHandlingConstants.ProblemDetailsHttpContextItemName] as ProblemDetails;

        ViewModel = new ErrorVm
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Title = problemDetails?.Title,
            Detail = problemDetails?.Detail,
        };
    }
}
