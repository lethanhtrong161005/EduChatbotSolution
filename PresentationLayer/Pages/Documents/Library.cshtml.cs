using AutoMapper;
using Business.Services.AI.Indexing;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NuGet.Packaging;
using Presentation.Background;
using Presentation.DTOs;
using Presentation.Extensions;

namespace Presentation.Pages.Documents;

/// <summary>
/// Displays the document library for lecturers and administrators.
/// Handles document API endpoints via handlers.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
[RequestSizeLimit(50L * 1024 * 1024)]
public class LibraryModel(
    ISubjectService subjectService,
    IChapterService chapterService,
    IDocumentService documentService,
    IDocumentFileReceptionService fileReceptionService,
    IDocumentFileService fileService,
    IResourceRealtimeNotifier notifier,
    IMapper mapper)
    : PageModel
{
    private const int DefaultPageSize = 10;

    private readonly ISubjectService _subjectService = subjectService;
    private readonly IChapterService _chapterService = chapterService;
    private readonly IDocumentService _documentService = documentService;
    private readonly IDocumentFileReceptionService _fileReceptionService = fileReceptionService;
    private readonly IDocumentFileService _fileService = fileService;
    private readonly IResourceRealtimeNotifier _notifier = notifier;
    private readonly IMapper _mapper = mapper;

    [FromHeader]
    public string CallerConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Loads subjects available to the current user.
    /// </summary>
    /// <returns>A task that renders the page.</returns>
    public async Task OnGetAsync()
    {
    }

    /// <summary>
    /// Returns all subjects accessible to the current user.
    /// Used by the subject grid.
    /// </summary>
    public async Task<IActionResult> OnGetSubjectsAsync(
        CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();

            // TODO: Move count to DB query
            var subjects = await _subjectService.GetAccessibleSubjectsAsync(
                userId,
                [
                    nameof(Subject.Chapters),
                    nameof(Subject.Chapters) + "." + nameof(Chapter.DocumentChapters),
                ],
                cxlTkn);

            return new JsonResult(_mapper.Map<List<SubjectSummaryDto>>(subjects));
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Returns detailed information for a single subject.
    /// Includes chapters for sidebar rendering.
    /// </summary>
    public async Task<IActionResult> OnGetSubjectAsync(
        [FromQuery] int id,
        CancellationToken cxlTkn)
    {
        try
        {
            if (id <= 0)
                return BadRequest();

            // TODO: Move count to DB query
            var subject = await _subjectService.GetSubjectByIdAsync(
                id,
                [
                    nameof(Subject.Chapters),
                    nameof(Subject.Chapters) + "." + nameof(Chapter.DocumentChapters),
                    nameof(Subject.Memberships),
                ],
                cxlTkn);

            if (subject == null)
                return NotFound();

            var canUpload = await _subjectService.IsChiefAsync(id, User.GetUserId(), cxlTkn);

            var dto = _mapper.Map<SubjectDetailsDto>(subject);
            dto.IsChief = canUpload;

            return new JsonResult(dto);
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Returns chapter headers for the chapter sidebar.
    /// </summary>
    public async Task<IActionResult> OnGetChaptersAsync(
        [FromQuery] int subjectId,
        CancellationToken cxlTkn)
    {
        if (subjectId <= 0)
            return BadRequest();

        var chapters = await _chapterService.GetBySubjectAsync(
            subjectId,
            [nameof(Chapter.DocumentChapters)],
            cxlTkn);

        return new JsonResult(_mapper.Map<List<ChapterSummaryDto>>(
            chapters.OrderBy(x => x.ChapterNumber)));
    }

    /// <summary>
    /// Returns chapter details for the chapter card.
    /// </summary>
    public async Task<IActionResult> OnGetChapterAsync(
        [FromQuery] int id,
        CancellationToken cxlTkn)
    {
        if (id <= 0)
            return BadRequest();

        // TODO: Move count to DB query
        var chapter = await _chapterService.GetByIdAsync(
            id,
            [nameof(Chapter.Subject), nameof(Chapter.DocumentChapters)],
            cxlTkn);

        if (chapter == null)
            return NotFound();

        return new JsonResult(_mapper.Map<ChapterDetailsDto>(chapter));
    }

    /// <summary>
    /// Returns paged documents for a subject.
    /// Supports search.
    /// </summary>
    public async Task<IActionResult> OnGetDocumentsAsync(
        [FromQuery] int subjectId,
        [FromQuery] int? chapterId,
        [FromQuery] int? pageSize,
        [FromQuery] int? pageIndex,
        [FromQuery] string? search = null,
        CancellationToken cxlTkn = default)
    {
        if (subjectId <= 0)
            return BadRequest();

        // TODO: Move pagination to DB query
        if (!pageSize.HasValue || pageSize <= 0)
            pageSize = DefaultPageSize;

        pageIndex = Math.Max(1, pageIndex ?? 1);

        var docs = (await _documentService.GetBySubjectAsync(
            subjectId,
            [nameof(Document.DocumentChapters), nameof(Document.Uploader)],
            cxlTkn))
            .ToList();

        if (chapterId.HasValue)
        {
            docs = [.. docs.Where(e => e.DocumentChapters.Select(e => e.ChapterId).Contains(chapterId.Value))];
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            docs = [.. docs.Where(x => x.Title.Contains(search, StringComparison.OrdinalIgnoreCase))];
        }

        var totalCount = docs.Count;

        var items = docs
            .OrderByDescending(x => x.UploadedAt)
            .Skip((pageIndex.Value - 1) * pageSize.Value)
            .Take(pageSize.Value)
            .ToList();

        return new JsonResult(new
        {
            Search = search,
            PageSize = pageSize,
            PageIndex = pageIndex,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize.Value),
            Items = _mapper.Map<List<DocumentFileDto>>(items),
        });
    }

    /// <summary>
    /// Uploads and processes document files.
    /// </summary>
    /// <param name="subjectId">The subject to upload to.</param>
    /// <param name="file">The file to upload.</param>
    /// <param name="chapterIds">The chapters the file pertain to.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnPostUploadAsync(
        [FromForm] int subjectId,
        [FromForm] List<int>? chapterIds,
        [FromForm] IFormFile file,
        CancellationToken cxlTkn)
    {
        try
        {
            if (file == null)
                return BadRequest("No files uploaded.");

            if (file.Length == 0)
                return BadRequest("Uploaded file is empty.");

            var userId = User.GetUserId();
            var canUpload = await _subjectService.IsChiefAsync(subjectId, userId, cxlTkn);
            if (!canUpload)
                return Forbid();

            var chapters = new List<Chapter>();
            var chapterIdSet = chapterIds?.ToHashSet() ?? [];

            if (chapterIdSet.Count > 0)
            {
                chapters = [.. await _chapterService.GetByIdsAsync(chapterIdSet, cancellationToken: cxlTkn)];

                if (chapters.Count != chapterIdSet.Count)
                    return BadRequest("No such chapter.");

                var violatingChapters = chapters.Where(e => e.SubjectId != subjectId).ToList();

                if (violatingChapters.Count > 0)
                    return BadRequest("Some chapters did not match target subject: " +
                        $"{string.Join(' ', violatingChapters.Select(e => $"'{e.Id}'"))}");
            }

            await using var fs = file.OpenReadStream();
            var result = await _fileReceptionService.ReceiveAsync(fs, file.FileName, cxlTkn);

            if (!result.Success)
            {
                return BadRequest($"Problem processing {file.FileName}: {string.Join(" • ", result.Errors)}");
            }

            var doc = new Document
            {
                SubjectId = subjectId,
                UploaderId = userId,
                Title = Path.GetFileNameWithoutExtension(file.FileName),
                OriginalFileName = file.FileName,
                FileType = result.FileType.Value,
                StorageLocator = null,
                StagingLocator = result.StagingLocator,
                StorageMethod = DocumentStorageMethod.Unspecified,
                FileSize = file.Length,
                Status = DocumentStatus.Received,
                UploadedAt = DateTime.UtcNow,
            };
            doc.Chapters.AddRange(chapters);

            var newDoc = await _documentService.CreateAsync(doc, cxlTkn);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Document,
                Action = ResourceAction.Created,
                ResourceId = doc.Id.ToString(),
                ResourceName = doc.Title,
                Properties =
                {
                    { nameof(Document.UploaderId) , doc.UploaderId.ToString() },
                },
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            var uploadJobId = BackgroundJob.Enqueue<DocumentPersistenceJob>(
                HangfireConstants.LowPriorityQueue,
                e => e.PersistAsync(doc.Id));

            var parseJobId = BackgroundJob.ContinueJobWith<IDocumentIndexer>(
                uploadJobId,
                HangfireConstants.LowPriorityQueue,
                e => e.ParseAsync(doc.Id));

            var chunkJobId = BackgroundJob.ContinueJobWith<IDocumentIndexer>(
                parseJobId,
                HangfireConstants.LowPriorityQueue,
                e => e.ChunkAsync(doc.Id));

            BackgroundJob.ContinueJobWith<IDocumentIndexer>(
                chunkJobId,
                HangfireConstants.LowPriorityQueue,
                e => e.EmbedAsync(doc.Id));

            var dtos = _mapper.Map<DocumentFileDto>(newDoc);
            return new JsonResult(dtos);
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Deletes a document, cancels indexing jobs, and removes the stored file.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnDeleteAsync(Guid id, CancellationToken cxlTkn)
    {
        try
        {
            var doc = await _documentService.GetByIdAsync(id, cancellationToken: cxlTkn);
            if (doc == null)
                return NotFound();

            var userId = User.GetUserId();
            var canUpload = await _subjectService.IsChiefAsync(doc.SubjectId, userId, cxlTkn);
            if (!canUpload)
                return Forbid();

            HangfireHelper.CancelJobs(doc.Id,
            [
                nameof(DocumentPersistenceJob.PersistAsync),
                nameof(IDocumentIndexer.ParseAsync),
                nameof(IDocumentIndexer.ChunkAsync),
                nameof(IDocumentIndexer.EmbedAsync),
            ]);

            var result = await _fileService.DeleteAsync(doc.Id, cxlTkn);
            if (!result.Success)
            {
                // Log and move on
            }

            await _documentService.DeleteAsync(doc.Id, cxlTkn);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Document,
                Action = ResourceAction.Deleted,
                ResourceId = doc.Id.ToString(),
                ResourceName = doc.Title,
                Properties =
                {
                    { nameof(Document.UploaderId) , doc.UploaderId.ToString() },
                },
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            return new JsonResult(new { Success = true });
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
    }
}
