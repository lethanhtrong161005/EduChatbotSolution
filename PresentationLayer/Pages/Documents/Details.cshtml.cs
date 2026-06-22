using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.ViewModels;
using System.Security.Claims;

namespace Presentation.Pages.Documents;

/// <summary>
/// Displays document details and handles comments for a document.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
public class DetailsModel(
    IDocumentService documentService,
    IMapper mapper) : PageModel
{
    private readonly IDocumentService _documentService = documentService;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets the document details view model rendered by the page.
    /// </summary>
    public DocumentDetailsVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Loads document metadata, chunks, and comments.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The details page or not found when the document does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(
            id,
            includeProperties:
            [
                nameof(Document.Chapter),
                nameof(Document.Uploader),
                nameof(Document.Chunks),
                nameof(Document.Comments),
            ],
            cancellationToken: cxlTkn);

        if (doc == null)
        {
            return NotFound();
        }

        ViewModel = _mapper.Map<DocumentDetailsVm>(doc);
        return Page();
    }

    /// <summary>
    /// Adds a comment to the current document and redirects back to the details page.
    /// </summary>
    /// <param name="id">The document identifier from the route.</param>
    /// <param name="documentId">The document identifier from the posted form.</param>
    /// <param name="content">The submitted comment text.</param>
    /// <returns>A redirect back to the document details page.</returns>
    public async Task<IActionResult> OnPostAddCommentAsync(Guid id, Guid documentId, string content)
    {
        var targetDocumentId = documentId == Guid.Empty ? id : documentId;

        if (string.IsNullOrWhiteSpace(content))
        {
            return RedirectToPage(new { id = targetDocumentId });
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdString, out var userId))
        {
            await _documentService.AddCommentAsync(targetDocumentId, userId, content);
        }

        return RedirectToPage(new { id = targetDocumentId });
    }
}
