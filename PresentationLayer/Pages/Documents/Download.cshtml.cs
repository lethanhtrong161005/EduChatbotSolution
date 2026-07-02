using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Documents;

/// <summary>
/// Handles document download requests.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Student)},{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
public class DownloadModel(
    IDocumentService documentService,
    IDocumentFileService fileService)
    : PageModel
{
    private readonly IDocumentService _documentService = documentService;
    private readonly IDocumentFileService _fileService = fileService;

    /// <summary>
    /// Downloads the original document file.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The document file or a not-found result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, cancellationToken: cxlTkn);

        if (doc == null)
            return NotFound();

        var result = await _fileService.Download(doc.Id, cxlTkn);
        if (!result.Success)
            return StatusCode(StatusCodes.Status500InternalServerError, "Failed to retrieve document file.");

        return File(
            fileStream: System.IO.File.OpenRead(result.FilePath),
            contentType: doc.ContentType,
            fileDownloadName: doc.OriginalFileName);
    }
}
