using DataAccess.UnitOfWork;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Extensions;
using Presentation.ViewModels;

namespace Presentation.Pages.Documents;

[Authorize(Roles = $"{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
public class EditModel(
    IDocumentService documentService,
    ISubjectService subjectService,
    IUnitOfWork unitOfWork) : PageModel
{
    private readonly IDocumentService _documentService = documentService;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    [BindProperty]
    public DocumentEditVm ViewModel { get; set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(
            id,
            includeProperties: [nameof(Document.Chapter)],
            cancellationToken: cxlTkn);

        if (doc == null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var isChief = await _subjectService.IsChiefAsync(doc.Chapter.SubjectId, userId, cxlTkn);

        if (!isChief)
        {
            return Forbid();
        }

        ViewModel = new DocumentEditVm
        {
            Id = doc.Id,
            Title = doc.Title,
            Description = doc.Description
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cxlTkn)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var doc = await _documentService.GetByIdAsync(
            ViewModel.Id,
            includeProperties: [nameof(Document.Chapter)],
            cancellationToken: cxlTkn);

        if (doc == null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var isChief = await _subjectService.IsChiefAsync(doc.Chapter.SubjectId, userId, cxlTkn);

        if (!isChief)
        {
            return Forbid();
        }

        doc.Title = ViewModel.Title;
        doc.Description = ViewModel.Description;

        await _documentService.UpdateAsync(doc, cxlTkn);

        return RedirectToPage("Details", new { id = doc.Id });
    }
}
