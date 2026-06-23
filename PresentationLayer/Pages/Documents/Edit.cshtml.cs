using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Extensions;
using Presentation.ViewModels;

namespace Presentation.Pages.Documents;

/// <summary>
/// Edits document title and description.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
public class EditModel(
    IDocumentService documentService,
    IMapper mapper) : PageModel
{
    private readonly IDocumentService _documentService = documentService;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets or sets the document edit form model bound to form post data.
    /// </summary>
    [BindProperty]
    public DocumentEditVm ViewModel { get; set; } = new();

    /// <summary>
    /// Loads document data into the edit form.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The edit page or not found when the document does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, null!, cxlTkn);
        if (doc == null)
            return NotFound();

        var userId = User.GetUserId();
        if (doc.UploaderId != userId && !User.IsInRole("Admin"))
            return Forbid();

        ViewModel = _mapper.Map<DocumentEditVm>(doc);
        return Page();
    }

    /// <summary>
    /// Updates the document title and description.
    /// </summary>
    /// <param name="id">The document identifier from the route.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A redirect to the document details page or the edit page on validation failure.</returns>
    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cxlTkn)
    {
        if (!ModelState.IsValid)
            return Page();

        var doc = await _documentService.GetByIdAsync(id, null!, cxlTkn);
        if (doc == null)
            return NotFound();

        var userId = User.GetUserId();
        if (doc.UploaderId != userId && !User.IsInRole("Admin"))
            return Forbid();

        doc.Title = ViewModel.Title;
        doc.Description = ViewModel.Description;

        await _documentService.UpdateAsync(doc, cxlTkn);
        return RedirectToPage("/Documents/Details", new { id = doc.Id });
    }
}
