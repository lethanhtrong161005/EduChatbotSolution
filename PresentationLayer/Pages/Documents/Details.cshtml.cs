using AutoMapper;
using DataAccess.UnitOfWork;
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
[Authorize(Roles = $"{nameof(UserRole.Student)},{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
public class DetailsModel(
    IDocumentService documentService,
    IUnitOfWork unitOfWork,
    IMapper mapper) : PageModel
{
    private readonly IDocumentService _documentService = documentService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets the document details view model rendered by the page.
    /// </summary>
    public DocumentDetailsVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Gets whether the physical file exists on this local server.
    /// </summary>
    public bool IsPhysicalFileAvailable { get; private set; } = true;

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
                nameof(Document.Comments) + "." + nameof(DocumentComment.User),
                nameof(Document.Comments) + "." + nameof(DocumentComment.User) + "." + nameof(ApplicationUser.SubjectMemberships),
                nameof(Document.ParsedSections),
            ], cxlTkn);
        if (doc == null)
            return NotFound();

        IsPhysicalFileAvailable = !string.IsNullOrWhiteSpace(doc.FilePath) && System.IO.File.Exists(doc.FilePath);

        // Verify if the user has permission to access this document
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Challenge();
        }

        var isAdmin = User.IsInRole(nameof(UserRole.Admin));
        if (!isAdmin)
        {
            var isMember = await _unitOfWork.SubjectMemberships.ExistsAsync(
                filter: m => m.UserId == userId && m.SubjectId == doc.Chapter.SubjectId,
                cancellationToken: cxlTkn);

            if (!isMember)
            {
                return Forbid();
            }
        }

        var vm = _mapper.Map<DocumentDetailsVm>(doc);

        if (doc.ParsedSections != null && doc.ParsedSections.Count > 0)
        {
            vm.ParsedSections = [.. doc.ParsedSections
                .OrderBy(s => s.SectionIndex)
                .Select(s => new ParsedSectionVm
                {
                    SectionIndex = s.SectionIndex,
                    PageNumber = s.PageNumber,
                    SectionTitle = s.SectionTitle,
                    Text = s.Text
                })];

            vm.ExtractedText = string.Join("\n\n", doc.ParsedSections.OrderBy(s => s.SectionIndex).Select(s => s.Text));
        }

        // Fallback for TXT/HTML files: read directly from file if ExtractedText is empty
        if (string.IsNullOrWhiteSpace(vm.ExtractedText) && System.IO.File.Exists(doc.FilePath))
        {
            try
            {
                if (doc.FileType == DocumentType.TXT || doc.FileType == DocumentType.HTML)
                {
                    vm.ExtractedText = await System.IO.File.ReadAllTextAsync(doc.FilePath, cxlTkn);
                }
            }
            catch
            {
                // Ignore read errors
            }
        }

        ViewModel = vm;

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

        var doc = await _documentService.GetByIdAsync(
            targetDocumentId,
            includeProperties: [nameof(Document.Chapter)]);

        if (doc == null)
        {
            return NotFound();
        }

        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Challenge();
        }

        var isAdmin = User.IsInRole(nameof(UserRole.Admin));
        if (!isAdmin)
        {
            var isMember = await _unitOfWork.SubjectMemberships.ExistsAsync(
                filter: m => m.UserId == userId && m.SubjectId == doc.Chapter.SubjectId);

            if (!isMember)
            {
                return Forbid();
            }
        }

        await _documentService.AddCommentAsync(targetDocumentId, userId, content);

        return RedirectToPage(new { id = targetDocumentId });
    }
}
