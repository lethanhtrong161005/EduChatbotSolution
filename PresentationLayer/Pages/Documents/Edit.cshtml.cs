using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
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
    ISubjectService subjectService,
    IResourceRealtimeNotifier notifier,
    IMapper mapper)
    : PageModel
{
    private readonly IDocumentService _documentService = documentService;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IResourceRealtimeNotifier _notifier = notifier;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets or sets the document edit form model bound to form post data.
    /// </summary>
    [BindProperty]
    public DocumentEditVm ViewModel { get; set; } = new();

    public int SubjectId { get; set; }

    public int ChapterId { get; set; }

    public Guid UploaderId { get; set; }

    public string ViewerMembershipId { get; set; } = string.Empty;

    [FromForm]
    public string CallerConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Loads document data into the edit form.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The edit page or not found when the document does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cxlTkn)
    {
        try
        {
            var doc = await _documentService.GetByIdAsync(
                    id,
                    includeProperties: [nameof(Document.Chapter)],
                    cancellationToken: cxlTkn);

            if (doc == null)
                return NotFound();

            var userId = User.GetUserId();

            var membership = await _subjectService.GetMembershipAsync(doc.Chapter.SubjectId, userId, cxlTkn);
            if (membership == null || membership.Role != MembershipRole.Chief)
                return Forbid();

            ViewerMembershipId = membership.Id.ToString();

            ViewModel = _mapper.Map<DocumentEditVm>(doc);
            SubjectId = doc.Chapter.Id;
            ChapterId = doc.ChapterId;
            UploaderId = doc.UploaderId;

            return Page();
        }
        catch (UserClaimException)
        {
            return Challenge();
        }
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
        {
            return Page();
        }

        var doc = await _documentService.GetByIdAsync(
            id,
            includeProperties: [nameof(Document.Chapter)],
            cancellationToken: cxlTkn);

        if (doc == null)
            return NotFound();

        var userId = User.GetUserId();

        if (!User.IsInRole(nameof(UserRole.Admin)))
        {
            var isChief = await _subjectService.IsChiefAsync(doc.Chapter.SubjectId, userId, cxlTkn);
            if (!isChief)
                return Forbid();
        }

        doc.Title = ViewModel.Title;
        doc.Description = ViewModel.Description;

        await _documentService.UpdateAsync(doc, cxlTkn);

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.Document,
            Action = ResourceAction.Updated,
            ResourceId = doc.Id.ToString(),
            ResourceName = doc.Title,
            Properties =
            {
                { nameof(Document.Chapter.SubjectId) , doc.Chapter.SubjectId.ToString() },
                { nameof(Document.ChapterId) , doc.ChapterId.ToString() },
                { nameof(Document.Chapter.ChapterNumber) , doc.Chapter.ChapterNumber?.ToString() ?? ""},
                { nameof(Document.UploaderId) , doc.UploaderId.ToString() },
            },
        };

        await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return RedirectToPage("/Documents/Details", new { id = doc.Id });
    }
}
