using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Presentation.Extensions;
using Presentation.RealtimeWeb;
using Presentation.ViewModels;

namespace Presentation.Pages.Documents;

/// <summary>
/// Edits document title and description.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
public class EditModel(
    IDocumentService documentService,
    ISubjectService subjectService,
    IHubContext<RealtimeHub, IRealtimeClient> hub,
    IMapper mapper) : PageModel
{
    private readonly IDocumentService _documentService = documentService;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IHubContext<RealtimeHub, IRealtimeClient> _hub = hub;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets or sets the document edit form model bound to form post data.
    /// </summary>
    [BindProperty]
    public DocumentEditVm ViewModel { get; set; } = new();

    public int SubjectId { get; set; }

    [FromForm]
    public string CallerSignalRConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Loads document data into the edit form.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The edit page or not found when the document does not exist.</returns>
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

        ViewModel = _mapper.Map<DocumentEditVm>(doc);
        SubjectId = doc.Chapter.Id;

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
        var isChief = await _subjectService.IsChiefAsync(doc.Chapter.SubjectId, userId, cxlTkn);

        if (!isChief)
            return Forbid();

        doc.Title = ViewModel.Title;
        doc.Description = ViewModel.Description;

        await _documentService.UpdateAsync(doc, cxlTkn);

        var groups = ResourceRelations.DocumentGroups.ToList();
        groups.Remove(HubGroups.Resource("document-edit"));
        var upd = new ResourceUpdate
        {
            ResourceType = "document",
            Action = "deleted",
            ResourceId = doc.Id.ToString(),
            AlternateResourceId = [doc.Chapter.SubjectId.ToString(), doc.UploaderId.ToString()],
            ResourceName = doc.Title,
        };
        await _hub.Clients.Groups(groups).ResourceChanged(upd);
        await _hub.Clients.GroupExcept(HubGroups.Resource("document-edit"), CallerSignalRConnectionId).ResourceChanged(upd);

        return RedirectToPage("/Documents/Details", new { id = doc.Id });
    }
}
