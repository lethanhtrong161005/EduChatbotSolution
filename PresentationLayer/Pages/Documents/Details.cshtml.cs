using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.DTOs;
using Presentation.Extensions;
using Presentation.ViewModels;
using System.Security.Claims;

namespace Presentation.Pages.Documents;

/// <summary>
/// Displays document details and handles comments for a document.
/// Provides chunk preview API via handlers.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Student)},{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
public class DetailsModel(
    ISubjectService subjectService,
    IDocumentService documentService,
    IDocumentFileService fileService,
    IResourceRealtimeNotifier notifier,
    IMapper mapper)
    : PageModel
{
    private const int PageSize = 10;

    private readonly ISubjectService _subjectService = subjectService;
    private readonly IDocumentService _documentService = documentService;
    private readonly IDocumentFileService _fileService = fileService;
    private readonly IResourceRealtimeNotifier _notifier = notifier;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets the document details view model rendered by the page.
    /// </summary>
    public DocumentDetailsVm ViewModel { get; private set; } = new();

    public bool IsPhysicalFileAvailable { get; private set; } = true;

    /// <summary>
    /// Gets whether the current user has permission to edit this document.
    /// </summary>
    public bool CanEdit { get; private set; } = false;

    public string ViewerMembershipId { get; set; } = string.Empty;

    [FromForm]
    public string CallerConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Loads document metadata, chunks, and comments.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The details page or not found when the document does not exist.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cxlTkn)
    {
        try
        {
            var doc = await _documentService.GetDocumentDetailsByIdAsync(id, cxlTkn);

            if (doc == null)
                return NotFound();

            IsPhysicalFileAvailable = await _fileService.Exists(doc.Id, cxlTkn);

            // Verify if the user has permission to access this document
            var isAdmin = User.IsInRole(nameof(UserRole.Admin));

            if (!isAdmin)
            {
                var userId = User.GetUserId();
                var membership = await _subjectService.GetMembershipAsync(doc.SubjectId, userId, cxlTkn);
                if (membership == null)
                    return Forbid();

                ViewerMembershipId = membership?.Id.ToString() ?? string.Empty;
                CanEdit = membership?.Role == MembershipRole.Chief;
            }
            else
            {
                ViewerMembershipId = string.Empty;
                CanEdit = true;
            }

            var vm = _mapper.Map<DocumentDetailsVm>(doc);

            // Fallback for TXT/HTML files: read directly from file if ExtractedText is empty
            if (string.IsNullOrWhiteSpace(vm.ExtractedText) && await _fileService.Exists(doc.Id, cxlTkn))
            {
                try
                {
                    if (doc.FileType == DocumentType.TXT || doc.FileType == DocumentType.HTML)
                    {
                        var result = await _fileService.Download(doc.Id, cxlTkn);
                        if (result.Success)
                            vm.ExtractedText = await System.IO.File.ReadAllTextAsync(result.FilePath, cxlTkn);
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
        catch (UserClaimException)
        {
            return Challenge();
        }
    }

    /// <summary>
    /// Returns a paginated preview of indexed chunks for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="pageIndex">The one-based page index requested by the client.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnGetChunksAsync(Guid id, [FromQuery] int pageIndex, CancellationToken cxlTkn)
    {
        var chunks = (PaginatedList<Chunk>)(PaginatedEnumerable<Chunk>)await _documentService.GetChunksAsync(
            id,
            PageSize,
            pageIndex,
            cxlTkn);

        var chunkDtos = _mapper.Map<List<ChunkPreviewDto>>(chunks);
        var pageDto = new ChunkPreviewPageDto
        {
            Chunks = chunkDtos,
            PageIndex = chunks.PageIndex,
            TotalPages = chunks.TotalPages,
        };

        return new JsonResult(pageDto);
    }

    /// <summary>
    /// Adds a comment to the current document and redirects back to the details page.
    /// </summary>
    /// <param name="id">The document identifier from the route.</param>
    /// <param name="documentId">The document identifier from the posted form.</param>
    /// <param name="content">The submitted comment text.</param>
    /// <returns>A redirect back to the document details page.</returns>
    public async Task<IActionResult> OnPostAddCommentAsync(Guid id, Guid documentId, string content, CancellationToken cxlTkn)
    {
        var targetDocumentId = documentId == Guid.Empty ? id : documentId;

        if (string.IsNullOrWhiteSpace(content))
        {
            return RedirectToPage(new { id = targetDocumentId });
        }

        var doc = await _documentService.GetByIdAsync(targetDocumentId, cancellationToken: cxlTkn);

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
            var isMember = await _subjectService.IsMemberAsync(doc.SubjectId, userId, cxlTkn);

            if (!isMember)
            {
                return Forbid();
            }
        }

        var comment = await _documentService.AddCommentAsync(targetDocumentId, userId, content);

        //var update = new ResourceUpdate
        //{
        //	ResourceType = ResourceType.Comment,
        //	Action = ResourceAction.Created,
        //	ResourceId = comment.Id.ToString(),
        //	Properties = {
        //		{ nameof(DocumentComment.DocumentId), comment.DocumentId.ToString() },
        //		{ nameof(DocumentComment.UserId), comment.UserId.ToString() },
        //	},
        //};

        //await _notifier.PushUpdateAsync(update, CallerConnectionId);

        return RedirectToPage(new { id = targetDocumentId });
    }
}
