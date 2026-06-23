using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Extensions;
using Presentation.ViewModels;

namespace Presentation.Pages.Documents;

/// <summary>
/// Displays the document library for students, lecturers, and administrators.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Student)},{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
public class LibraryModel(
    ISubjectService subjectService,
    IMapper mapper) : PageModel
{
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets the document library view model rendered by the page.
    /// </summary>
    public DocumentLibraryVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Loads subjects available to the current user.
    /// </summary>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A task that renders the page.</returns>
    public async Task OnGetAsync(CancellationToken cxlTkn)
    {
        var userId = User.GetUserId();
        var accessibleSubjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cxlTkn);

        ViewModel = new DocumentLibraryVm
        {
            Subjects = _mapper.Map<List<SubjectLookupVm>>(accessibleSubjects),
        };
    }
}
