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
using Microsoft.Net.Http.Headers;
using Presentation.Constants;
using Presentation.DTOs;
using Presentation.Extensions;
using Presentation.Utils;
using Presentation.ViewModels;

namespace Presentation.Controllers;

/// <summary>
/// Exposes document JSON, upload, download, and display endpoints.
/// Razor Pages handle document library and detail screens.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
[Route("documents")]
public class DocumentsController(
    ISubjectService subjectService,
    IChapterService chapterService,
    IDocumentService documentService,
    IMapper mapper) : Controller
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
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Checks whether the current user can upload documents to the subject.
    /// </summary>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A JSON result containing the upload permission.</returns>
    [HttpGet("can-upload")]
    public async Task<IActionResult> CanUpload(int subjectId, CancellationToken cxlTkn)
    {
        if (subjectId <= 0)
        {
            return BadRequest(new { Error = "Subject ID is missing or invalid." });
        }

        Guid userId;
        try
        {
            userId = User.GetUserId();
        }
        catch
        {
            return BadRequest(new { Error = "Could not determine user ID." });
        }

        var canUpload = await _subjectService.IsChiefAsync(subjectId, userId, cxlTkn);
        return Json(new { canUpload });
    }

    /// <summary>
    /// Returns chapters for a selected subject.
    /// </summary>
    /// <param name="subjectId">The subject identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A JSON list of chapter lookup items.</returns>
    [HttpGet("get-chapters")]
    public async Task<IActionResult> GetChapters(int subjectId, CancellationToken cxlTkn)
    {
        var chapters = await _chapterService.GetBySubjectAsync(subjectId, cxlTkn);
        return Json(_mapper.Map<List<ChapterLookupDto>>(chapters));
    }

    /// <summary>
    /// Returns document files for a selected subject or chapter.
    /// </summary>
    /// <param name="subjectId">Optional subject identifier.</param>
    /// <param name="chapterId">Optional chapter identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A JSON list of document file items.</returns>
    [HttpGet("get-files")]
    public async Task<IActionResult> GetFiles(int? subjectId, int? chapterId, CancellationToken cxlTkn)
    {
        if (subjectId == null && chapterId == null)
        {
            return BadRequest(new { Error = "Either subject or chapter ID must be provided." });
        }

        var docs = chapterId.HasValue
            ? await _documentService.GetByChapterAsync(chapterId.Value, cxlTkn)
            : await _documentService.GetBySubjectAsync(subjectId!.Value, cxlTkn);

        return Json(_mapper.Map<List<DocumentFileDto>>(docs));
    }

    /// <summary>
    /// Downloads the original document file.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The document file or a not-found result.</returns>
    [HttpGet("download/{id:guid}")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, cancellationToken: cxlTkn);

        if (doc == null)
        {
            return NotFound();
        }

        return File(
            fileStream: System.IO.File.OpenRead(doc.FilePath),
            contentType: doc.ContentType,
            fileDownloadName: doc.OriginalFileName);
    }

    /// <summary>
    /// Displays a document inline when supported by the browser or Office viewer.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>An inline file, download file, redirect, or not-found result.</returns>
    [HttpGet("display/{id:guid}")]
    public async Task<IActionResult> Display(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, cancellationToken: cxlTkn);

        if (doc == null)
        {
            return NotFound();
        }

        if (doc.FileType == DocumentType.PDF || doc.FileType == DocumentType.DOCX || doc.FileType == DocumentType.TXT || doc.FileType == DocumentType.HTML)
        {
            var contentDisposition = ContentDispositionHeaderValue.Parse($"inline; filename={doc.OriginalFileName}");

            Response.Headers.ContentDisposition = contentDisposition.ToString();

            return File(
                fileStream: System.IO.File.OpenRead(doc.FilePath),
                contentType: doc.ContentType);
        }

        return File(
            fileStream: System.IO.File.OpenRead(doc.FilePath),
            contentType: doc.ContentType,
            fileDownloadName: doc.OriginalFileName);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, cancellationToken: cxlTkn);
        if (doc == null)
            return NotFound();

        var vm = new DocumentEditVm
        {
            Id = doc.Id,
            Title = doc.Title,
            Description = doc.Description
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(DocumentEditVm vm, CancellationToken cxlTkn)
    {
        if (!ModelState.IsValid)
        {
            return View(vm);
        }

        var doc = await _documentService.GetByIdAsync(vm.Id, cancellationToken: cxlTkn);
        if (doc == null)
            return NotFound();

        doc.Title = vm.Title;
        doc.Description = vm.Description;

        await _documentService.UpdateAsync(doc, cxlTkn);

        return RedirectToPage("/Documents/Details", new { id = doc.Id });
    }

    /// <summary>
    /// Deletes a document, cancels indexing jobs, and removes the stored file.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>An OK result or not-found result.</returns>
    [HttpDelete("delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, cancellationToken: cxlTkn);

        if (doc == null)
        {
            return NotFound();
        }

        HangfireHelper.CancelJobs(doc.Id,
        [
            nameof(DocumentIndexer.ParseAsync),
            nameof(DocumentIndexer.ChunkAsync),
            nameof(DocumentIndexer.EmbedAsync)
        ]);

        await _documentService.DeleteAsync(doc.Id, cxlTkn);
        System.IO.File.Delete(doc.FilePath);

        return Ok();
    }

    [HttpPost]
    [RequestSizeLimit(100L * 1024 * 1024)]
    public async Task<IActionResult> Upload(int chapterId, List<IFormFile> files, CancellationToken cxlTkn)
    {
        if (files.Count == 0)
        {
            return BadRequest("No files uploaded.");
        }

        var userId = User.GetUserId();

        var chapter = await _chapterService.GetByIdAsync(chapterId, cxlTkn);
        if (chapter == null)
        {
            return BadRequest("No such chapter.");
        }

        var canUpload = await _subjectService.IsChiefAsync(chapter.SubjectId, userId, cxlTkn);
        if (!canUpload)
        {
            return Unauthorized("You do not have upload privilege for this subject.");
        }

        var docs = new List<Document>();

        foreach (var file in files)
        {
            var extension = Path.GetExtension(file.FileName);
            var storageName = $"{Guid.NewGuid()}{extension}";

            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest($"{file.FileName} is not supported");
            }

            var tempDir = Path.Combine(Path.GetTempPath(), AppConstants.AppDir, AppConstants.FileSubdirUploaded);
            Directory.CreateDirectory(tempDir);

            var fullPath = Path.Combine(tempDir, storageName);
            await using var fs = System.IO.File.Create(fullPath);

            await file.CopyToAsync(fs, cxlTkn);

            var mime = MimeGuesser.GuessFileType(fullPath);
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
        return Json(result);
    }

    /// <summary>
    /// Returns a paginated preview of indexed chunks for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="pageIndex">The one-based page index requested by the client.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>A JSON page of chunk preview items.</returns>
    [HttpGet("chunks")]
    public async Task<IActionResult> Chunks(Guid documentId, int pageIndex, CancellationToken cxlTkn)
    {
        var chunks = (PaginatedList<Chunk>)(PaginatedEnumerable<Chunk>)await _documentService.GetChunksAsync(
            documentId,
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

        return Json(pageDto);
    }
}
