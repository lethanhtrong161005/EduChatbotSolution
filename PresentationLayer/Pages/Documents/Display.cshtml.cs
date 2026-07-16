using Domain.Contracts;
using Domain.Entities;
using Domain.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Net.Http.Headers;

namespace Presentation.Pages.Documents;

/// <summary>
/// Handles document display requests (inline or download).
/// </summary>
[Authorize]
public class DisplayModel(
    IDocumentService documentService,
    IDocumentFileService fileService)
    : PageModel
{
    private readonly IDocumentService _documentService = documentService;
    private readonly IDocumentFileService _fileService = fileService;

    /// <summary>
    /// Displays a document inline when supported by the browser or Office viewer.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>An inline file, download file, or not-found result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, cancellationToken: cxlTkn);

        if (doc == null)
            return NotFound();

        var result = await _fileService.OpenReadAsync(doc.Id, cxlTkn);
        if (!result.Success)
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve document file.");

        if (doc.FileType == FileType.PDF || doc.FileType == FileType.DOCX || doc.FileType == FileType.TXT || doc.FileType == FileType.HTML)
        {
            var contentDisposition = ContentDispositionHeaderValue.Parse($"inline; filename=\"{doc.OriginalFileName}\"");
            Response.Headers.ContentDisposition = contentDisposition.ToString();

            return File(
                fileStream: result.FileStream,
                contentType: doc.ContentType);
        }

        return File(
            fileStream: result.FileStream,
            contentType: doc.ContentType,
            fileDownloadName: doc.OriginalFileName);
    }
}
