using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    IHubContext<ResourceHub, IResourceClient> hub,
    IMapper mapper) : PageModel
{
    private readonly IDocumentService _documentService = documentService;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IHubContext<ResourceHub, IResourceClient> _hub = hub;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets or sets the document edit form model bound to form post data.
    /// </summary>
    [BindProperty]
    public DocumentEditVm ViewModel { get; set; } = new();

    public int SubjectId { get; set; }

    public int ChapterId { get; set; }

    public Guid UploaderId { get; set; }

    [FromForm]
    public string CallerSignalRConnectionId { get; set; } = string.Empty;

    private static List<string> OtherDocumentGroups(Guid docId, int subjectId, Guid uploaderId)
    {
        var groups = NotificationTargets.Document(docId, subjectId, uploaderId).ToList();
        groups.Remove(ThisGroup(docId));
        return groups;
    }

    private static string ThisGroup(Guid docId) =>
        HubGroups.Resource(PageTypes.DocumentEdit, docId.ToString());

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
        ChapterId = doc.ChapterId;
        UploaderId = doc.UploaderId;

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

        var upd = new ResourceUpdate
        {
            ResourceType = ResourceTypes.Comment,
            Action = Actions.Deleted,
            ResourceId = doc.Id.ToString(),
            ResourceName = doc.Title,
            Properties =
            {
                { nameof(Document.Chapter.SubjectId) , doc.Chapter.SubjectId.ToString() },
                { nameof(Document.UploaderId) , doc.UploaderId.ToString() },
            },
        };

        await _hub.Clients
            .Groups(OtherDocumentGroups(doc.Id, doc.Chapter.SubjectId, doc.UploaderId))
            .ResourceChanged(upd);

        await _hub.Clients
            .GroupExcept(ThisGroup(doc.Id), CallerSignalRConnectionId)
            .ResourceChanged(upd);

        return RedirectToPage("/Documents/Details", new { id = doc.Id });
    }
}
