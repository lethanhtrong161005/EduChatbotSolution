using AutoMapper;
using Business.Services.AI.Indexing;
using Domain.Common;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
    ITemporaryStorageService tempStorageService,
    IDocumentFileService fileService,
    IResourceRealtimeNotifier notifier,
    IMapper mapper)
    : PageModel
{
    private const int DefaultPageSize = 10;

    private readonly ISubjectService _subjectService = subjectService;
    private readonly IChapterService _chapterService = chapterService;
    private readonly IDocumentService _documentService = documentService;
    private readonly ITemporaryStorageService _tempStorageService = tempStorageService;
    private readonly IDocumentFileService _fileService = fileService;
    private readonly IResourceRealtimeNotifier _notifier = notifier;
    private readonly IMapper _mapper = mapper;

    [FromHeader]
    public string CallerConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Loads subjects available to the current user.
    /// </summary>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A task that renders the page.</returns>
    public async Task OnGetAsync()
    {
    }

    #region OBSOLETE

    public async Task<IActionResult> OnGetGetSubjectsAsync(CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();
            var accessibleSubjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cancellationToken: cxlTkn);
            return new JsonResult(_mapper.Map<List<SubjectSidebarDto>>(accessibleSubjects));
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Checks whether the current user can upload documents to the subject.
    /// </summary>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnGetCanUploadAsync([FromQuery] int subjectId, CancellationToken cxlTkn)
    {
        if (subjectId <= 0)
            return BadRequest(new { error = "Subject ID is missing or invalid." });

        var userId = User.GetUserId();
        var canUpload = await _subjectService.IsChiefAsync(subjectId, userId, cxlTkn);
        return new JsonResult(new { canUpload });
    }

    /// <summary>
    /// Returns chapters for a selected subject.
    /// </summary>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnGetGetChaptersAsync([FromQuery] int subjectId, CancellationToken cxlTkn)
    {
        var chapters = await _chapterService.GetBySubjectAsync(subjectId, cancellationToken: cxlTkn);
        return new JsonResult(_mapper.Map<List<ChapterSidebarDto>>(chapters));
    }

    /// <summary>
    /// Returns document files for a selected subject or chapter.
    /// </summary>
    /// <param name="subjectId">Optional subject identifier.</param>
    /// <param name="chapterId">Optional chapter identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnGetGetFilesAsync([FromQuery] int? subjectId, [FromQuery] int? chapterId, CancellationToken cxlTkn)
    {
        if (subjectId == null && chapterId == null)
            return BadRequest(new { error = "Either subject or chapter ID must be provided." });

        var docs = chapterId.HasValue
            ? await _documentService.GetByChapterAsync(chapterId.Value, cancellationToken: cxlTkn)
            : await _documentService.GetBySubjectAsync(subjectId!.Value, cancellationToken: cxlTkn);

        return new JsonResult(_mapper.Map<List<DocumentFileDto>>(docs));
    }

    #endregion

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
                    nameof(Subject.Chapters) + "." + nameof(Chapter.Documents),
                ],
                cxlTkn);

            return new JsonResult(_mapper.Map<List<SubjectSidebarDto>>(subjects));
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
                    nameof(Subject.Chapters) + "." + nameof(Chapter.Documents),
                    nameof(Subject.Memberships),
                ],
                cxlTkn);

            if (subject == null)
                return NotFound();

            var chapters = await _chapterService.GetBySubjectAsync(
                id,
                [nameof(Chapter.Documents)],
                cxlTkn);

            var canUpload = await _subjectService.IsChiefAsync(id, User.GetUserId(), cxlTkn);

            return new JsonResult(new
            {
                Subject = _mapper.Map<SubjectDetailsDto>(subject),

                Chapters = _mapper.Map<List<ChapterSidebarDto>>(
                    chapters
                    .OrderBy(x => x.ChapterNumber ?? int.MaxValue)
                    .ThenBy(x => x.Name)),

                CanUpload = canUpload,
            });
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
        [FromQuery] int id,
        CancellationToken cxlTkn)
    {
        if (id <= 0)
            return BadRequest();

        var chapters = await _chapterService.GetBySubjectAsync(
            id,
            [nameof(Chapter.Documents)],
            cxlTkn);

        return new JsonResult(_mapper.Map<List<ChapterSidebarDto>>(
            chapters
            .OrderBy(x => x.ChapterNumber ?? int.MaxValue)
            .ThenBy(x => x.Name)));
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

        var chapter = await _chapterService.GetByIdAsync(
            id,
            [nameof(Chapter.Subject), nameof(Chapter.Documents)],
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
            [nameof(Document.Chapter), nameof(Document.Uploader)],
            cxlTkn))
            .ToList();

        if (chapterId.HasValue)
        {
            docs = [.. docs.Where(e => e.ChapterId == chapterId.Value)];
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
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize.Value),
            Items = _mapper.Map<List<DocumentFileDto>>(items),
        });
    }

    /// <summary>
    /// Uploads and processes document files.
    /// </summary>
    /// <param name="chapterId">The chapter to upload to.</param>
    /// <param name="files">The files to upload.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnPostUploadAsync([FromForm] int chapterId, [FromForm] List<IFormFile> files, CancellationToken cxlTkn)
    {
        try
        {
            if (files.Count == 0)
                return BadRequest("No files uploaded.");

            var userId = User.GetUserId();

            var chapter = await _chapterService.GetByIdAsync(chapterId, cancellationToken: cxlTkn);
            if (chapter == null)
                return BadRequest("No such chapter.");

            var canUpload = await _subjectService.IsChiefAsync(chapter.SubjectId, userId, cxlTkn);
            if (!canUpload)
                return Forbid();

            var docs = new List<Document>();
            var problems = new List<string>();

            foreach (var file in files)
            {
                await using var fs = file.OpenReadStream();
                var result = await _tempStorageService.ValidateAndSave(fs, file.FileName, cxlTkn);

                if (!result.Success)
                {
                    problems.Add($"Problem processing {file.FileName}: {string.Join(" • ", result.Errors)}");
                    continue;
                }

                docs.Add(new Document
                {
                    ChapterId = chapterId,
                    UploaderId = userId,
                    Title = Path.GetFileNameWithoutExtension(file.FileName),
                    FileName = Path.GetFileName(result.FilePath),
                    OriginalFileName = file.FileName,
                    FileType = result.FileType.Value,
                    FilePath = result.FilePath,
                    FileSize = file.Length,
                    Status = DocumentStatus.Uploaded,
                    UploadedAt = DateTime.UtcNow,
                });
            }

            if (problems.Count > 0)
                return BadRequest(problems);

            var newDocs = await _documentService.CreateRange(docs, cxlTkn);

            foreach (var doc in newDocs)
            {
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
            }

            foreach (var doc in newDocs)
            {
                var uploadJobId = BackgroundJob.Enqueue<IDocumentFileService>(
                    HangfireConstants.LowPriorityQueue,
                    e => e.Upload(doc.Id));

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
            }

            var dtos = _mapper.Map<List<DocumentFileDto>>(newDocs);
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
        var doc = await _documentService.GetByIdAsync(id, cancellationToken: cxlTkn);

        if (doc == null)
            return NotFound();

        HangfireHelper.CancelJobs(doc.Id,
        [
            nameof(DocumentIndexer.ParseAsync),
            nameof(DocumentIndexer.ChunkAsync),
            nameof(DocumentIndexer.EmbedAsync)
        ]);

        var result = await _fileService.Delete(doc.Id, cxlTkn);
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

        return new JsonResult(new { success = true });
    }
}
