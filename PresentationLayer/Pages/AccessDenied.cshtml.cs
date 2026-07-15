using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.ViewModels;
using System.Diagnostics;

namespace Presentation.Pages;

public class AccessDeniedModel : PageModel
{
    public ErrorVm ViewModel { get; private set; } = new();

    public void OnGet()
    {
        ViewModel = new ErrorVm
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Title = "Access Denied",
            Detail = "You are not qualified to access this resource.",
        };
    }
}
