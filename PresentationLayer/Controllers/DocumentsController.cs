using AutoMapper;
using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Domain.Utils;
using Hangfire;
using HeyRed.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Extensions;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize]
public class DocumentsController(
    ISubjectService subjectService,
    IChapterService chapterService,
    IDocumentService documentService,
    IMapper mapper
    ) : Controller
{
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IChapterService _chapterService = chapterService;
    private readonly IDocumentService _documentService = documentService;
    private readonly IMapper _mapper = mapper;

    [HttpGet]
    public async Task<IActionResult> Library(CancellationToken cxlTkn)
    {
        var userId = User.GetUserId();

        var accessibleSubjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cxlTkn);

        var vm = new DocumentLibraryVm
        {
            Subjects = _mapper.Map<List<SubjectLookupVm>>(accessibleSubjects),
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> CanUpload(int subjectId, CancellationToken cxlTkn)
    {
        if (subjectId <= 0)
            return BadRequest(new { Error = "Subject ID is missing or invalid." });

        Guid userId;
        try { userId = User.GetUserId(); }
        catch { return BadRequest(new { Error = "Could not determine user ID." }); }

        var canUpload = await _subjectService.IsChiefAsync(subjectId, userId, cxlTkn);
        return Json(new { canUpload });
    }

    [HttpGet]
    public async Task<IActionResult> GetChapters(int subjectId, CancellationToken cxlTkn)
    {
        var chapters = await _chapterService.GetBySubjectAsync(subjectId, cxlTkn);
        return Json(_mapper.Map<List<ChapterLookupVm>>(chapters));
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles(int? subjectId, int? chapterId, CancellationToken cxlTkn)
    {
        if (subjectId == null && chapterId == null)
            return BadRequest(new { Error = "Either subject or chapter ID must be provided." });

        var docs = chapterId.HasValue
            ? await _documentService.GetByChapterAsync(chapterId.Value, cxlTkn)
            : await _documentService.GetBySubjectAsync(subjectId!.Value, cxlTkn);

        return Json(_mapper.Map<List<DocumentFileVm>>(docs));
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, cxlTkn);

        if (doc == null)
            return NotFound();

        return File(fileStream: System.IO.File.OpenRead(doc.FilePath), doc.ContentType);
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, cxlTkn);

        if (doc == null)
            return NotFound();

        await _documentService.DeleteAsync(doc.Id, cxlTkn);
        System.IO.File.Delete(doc.FilePath);

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cxlTkn)
    {
        return PartialView("_ChunkPreview");
    }

    [HttpPost]
    [RequestSizeLimit(100L * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        int chapterId,
        List<IFormFile> files,
        CancellationToken cxlTkn)
    {
        if (files.Count == 0)
            return BadRequest("No files uploaded.");

        var userId = User.GetUserId();

        var chapter = await _chapterService.GetByIdAsync(chapterId, cxlTkn);
        if (chapter == null)
            return BadRequest("No such chapter.");

        var canUpload = await _subjectService.IsChiefAsync(chapter.SubjectId, userId, cxlTkn);
        if (!canUpload)
            return Unauthorized("You do not have upload privilege for this subject.");

        var docs = new List<Document>();

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName);
            var storageName = $"{Guid.NewGuid()}{extension}";

            if (!AllowedExtensions.Contains(extension))
                return BadRequest($"{file.FileName} is not supported");

            var tempDir = Path.Combine(Path.GetTempPath(), "EduChatAI", AppConstants.FileSubdirUploaded);
            Directory.CreateDirectory(tempDir);

            var fullPath = Path.Combine(tempDir, storageName);
            await using var fs = System.IO.File.Create(fullPath);

            await file.CopyToAsync(fs, cxlTkn);

            var mime = MimeGuesser.GuessFileType(fullPath);

            // FIXME: Studpidly insecure.
            if (mime is { MimeType: "inode/x-empty", Extension: "bin" })
            {
                mime = new FileType("text/plain", "txt");
            }

            if (!AllowedMimeTypes.Contains(mime.MimeType))
            {
                System.IO.File.Delete(fullPath);
                foreach (var d in docs)
                {
                    System.IO.File.Delete(d.FilePath);
                }

                return BadRequest(
                    $"{file.FileName} is not supported.");
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
            var parseJobId =
                BackgroundJob.Enqueue<IDocumentIndexer>(
                    e => e.ParseAsync(doc.Id));

            var chunkJobId =
                BackgroundJob.ContinueJobWith<IDocumentIndexer>(
                    parseJobId,
                    e => e.ChunkAsync(doc.Id));

            BackgroundJob.ContinueJobWith<IDocumentIndexer>(
                chunkJobId,
                e => e.EmbedAsync(doc.Id));
        }

        var result = _mapper.Map<List<DocumentFileVm>>(newDocs);
        return Ok(result);
    }

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".pdf",
        ".docx",
        ".pptx",
        ".txt",
        ".html",
    ];

    private static readonly HashSet<string>
    AllowedMimeTypes =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "text/html",
    ];

    private const int PageSize = 10;


    [HttpGet]
    public async Task<IActionResult> Chunks(Guid documentId, int pageIndex, CancellationToken cxlTkn)
    {
        var chunks = (PaginatedList<Chunk>)await _documentService.GetChunksAsync(documentId, PageSize, pageIndex, cxlTkn);

        var chunkVms = _mapper.Map<List<ChunkPreviewVm>>(chunks);
        var pageVm = new ChunkPreviewPageVm
        {
            Chunks = chunkVms,
            PageIndex = chunks.PageIndex,
            TotalPages = chunks.TotalPages,
        };

        return Json(pageVm);
    }
}
