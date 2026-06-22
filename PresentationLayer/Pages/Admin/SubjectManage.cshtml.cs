using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.ViewModels;

namespace Presentation.Pages.Admin;

/// <summary>
/// Displays the administrator subject management page with filters and pagination.
/// </summary>
[Authorize(Roles = "Admin")]
public class SubjectManageModel(ISubjectService subjectService) : PageModel
{
    private readonly ISubjectService _subjectService = subjectService;

    /// <summary>
    /// Gets the subject management view model rendered by the page.
    /// </summary>
    public AdminSubjectListVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Loads subjects for the subject management page.
    /// </summary>
    /// <param name="code">Optional subject code filter.</param>
    /// <param name="name">Optional subject name filter.</param>
    /// <param name="limit">The requested page size.</param>
    /// <param name="offset">The zero-based record offset.</param>
    /// <returns>A task that renders the page.</returns>
    public async Task OnGetAsync(string? code, string? name, int limit = 10, int offset = 0)
    {
        var subjects = await _subjectService.GetPagedSubjectsAsync(code, name, limit, offset);

        ViewModel = new AdminSubjectListVm
        {
            CodeFilter = code,
            NameFilter = name,
            Limit = limit,
            Offset = offset,
            Subjects = subjects,
        };
    }
}
