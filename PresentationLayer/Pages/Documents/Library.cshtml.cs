using AutoMapper;
using Business.Services.AI.Indexing;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Domain.Utils;
using Hangfire;
using HeyRed.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Presentation.Constants;
using Presentation.DTOs;
using Presentation.Extensions;
using Presentation.RealtimeWeb;
using Presentation.Utils;
using Presentation.ViewModels;

namespace Presentation.Pages.Documents;

/// <summary>
/// Displays the document library for lecturers and administrators.
/// Handles document API endpoints via handlers.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
[RequestSizeLimit(100L * 1024 * 1024)]
public class LibraryModel(
    ISubjectService subjectService,
    IChapterService chapterService,
    IDocumentService documentService,
    IHubContext<RealtimeHub, IRealtimeClient> hub,
    IMapper mapper) : PageModel
{
    private const int PageSize = 10;

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".pdf",
        ".docx",
        ".pptx",
        ".txt",
        ".html",
    ];

    private static readonly HashSet<string> AllowedMimeTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "text/html",
    ];

    private readonly ISubjectService _subjectService = subjectService;
    private readonly IChapterService _chapterService = chapterService;
    private readonly IDocumentService _documentService = documentService;
    private readonly IHubContext<RealtimeHub, IRealtimeClient> _hub = hub;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets the document library view model rendered by the page.
    /// </summary>
    public DocumentLibraryVm ViewModel { get; private set; } = new();

    [FromHeader]
    public string CallerSignalRConnectionId { get; set; } = string.Empty;

    private static List<string> OtherDocumentGroups
    {
        get
        {
            var groups = ResourceRelations.DocumentGroups.ToList();
            groups.Remove(ThisGroup);
            return groups;
        }
    }

    private static string ThisGroup => HubGroups.Resource("document-library");

    /// <summary>
    /// Loads subjects available to the current user.
    /// </summary>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A task that renders the page.</returns>
    public async Task OnGetAsync(CancellationToken cxlTkn)
    {
        var userId = User.GetUserId();
        var accessibleSubjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cxlTkn);

        ViewModel = new DocumentLibraryVm
        {
            Subjects = _mapper.Map<List<SubjectLookupVm>>(accessibleSubjects),
        };
    }

    public async Task<IActionResult> OnGetGetSubjectsAsync([FromQuery] int subjectId, CancellationToken cxlTkn)
    {
        var userId = User.GetUserId();
        var accessibleSubjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cxlTkn);

        return new JsonResult(_mapper.Map<List<SubjectLookupVm>>(accessibleSubjects));
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
        var chapters = await _chapterService.GetBySubjectAsync(subjectId, cxlTkn);
        return new JsonResult(_mapper.Map<List<ChapterLookupDto>>(chapters));
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
            ? await _documentService.GetByChapterAsync(chapterId.Value, cxlTkn)
            : await _documentService.GetBySubjectAsync(subjectId!.Value, cxlTkn);

        return new JsonResult(_mapper.Map<List<DocumentFileDto>>(docs));
    }

    /// <summary>
    /// Uploads and processes document files.
    /// </summary>
    /// <param name="chapterId">The chapter to upload to.</param>
    /// <param name="files">The files to upload.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnPostUploadAsync([FromForm] int chapterId, [FromForm] List<IFormFile> files, CancellationToken cxlTkn)
    {
        if (files.Count == 0)
            return BadRequest("No files uploaded.");

        var userId = User.GetUserId();

        var chapter = await _chapterService.GetByIdAsync(chapterId, cxlTkn);
        if (chapter == null)
            return BadRequest("No such chapter.");

        var canUpload = await _subjectService.IsChiefAsync(chapter.SubjectId, userId, cxlTkn);
        if (!canUpload)
            return Unauthorized();

        var docs = new List<Document>();

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName);
            var storageName = $"{Guid.NewGuid()}{extension}";

            if (!AllowedExtensions.Contains(extension))
                return BadRequest($"{file.FileName} is not supported");

            var tempDir = Path.Combine(Path.GetTempPath(), AppConstants.AppDir, AppConstants.FileSubdirUploaded);
            Directory.CreateDirectory(tempDir);

            var fullPath = Path.Combine(tempDir, storageName);
            await using var fs = System.IO.File.Create(fullPath);
            await file.CopyToAsync(fs, cxlTkn);

            var mime = MimeGuesser.GuessFileType(fullPath);
            if (mime is { MimeType: "inode/x-empty", Extension: "bin" })
                mime = new FileType("text/plain", "txt");

            if (!AllowedMimeTypes.Contains(mime.MimeType))
            {
                System.IO.File.Delete(fullPath);
                foreach (var d in docs)
                    System.IO.File.Delete(d.FilePath);
                return BadRequest($"{file.FileName} is not supported.");
            }

            var doc = new Document
            {
                ChapterId = chapterId,
                UploaderId = userId,
                Title = Path.GetFileNameWithoutExtension(file.FileName),
                FileName = storageName,
                OriginalFileName = file.FileName,
                FileType = FileHelper.ParseFileType(extension),
                FilePath = fullPath,
                FileSize = file.Length,
                Status = DocumentStatus.Uploaded,
                UploadedAt = DateTime.UtcNow,
            };

            docs.Add(doc);
        }

        var newDocs = await _documentService.CreateRange(docs, cxlTkn);

        foreach (var doc in newDocs)
        {
            var upd = new ResourceUpdate
            {
                ResourceType = "document",
                Action = "deleted",
                ResourceId = doc.Id.ToString(),
                AlternateResourceId = [null, doc.UploaderId.ToString()],
                ResourceName = doc.Title,
            };
            await _hub.Clients.Groups(OtherDocumentGroups).ResourceChanged(upd);
            await _hub.Clients.GroupExcept(ThisGroup, CallerSignalRConnectionId).ResourceChanged(upd);
        }

        foreach (var doc in newDocs)
        {
            var parseJobId = BackgroundJob.Enqueue<IDocumentIndexer>(
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

        var result = _mapper.Map<List<DocumentFileDto>>(newDocs);
        return new JsonResult(result);
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

        await _documentService.DeleteAsync(doc.Id, cxlTkn);

        System.IO.File.Delete(doc.FilePath);

        var upd = new ResourceUpdate
        {
            ResourceType = "document",
            Action = "deleted",
            ResourceId = doc.Id.ToString(),
            AlternateResourceId = [null, doc.UploaderId.ToString()],
            ResourceName = doc.Title,
        };
        await _hub.Clients.Groups(OtherDocumentGroups).ResourceChanged(upd);
        await _hub.Clients.GroupExcept(ThisGroup, CallerSignalRConnectionId).ResourceChanged(upd);

        return new JsonResult(new { success = true });
    }
}
