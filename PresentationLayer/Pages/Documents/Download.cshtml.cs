using Domain.Common;
using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Presentation.Pages.Documents;

/// <summary>
/// Handles document download requests.
/// </summary>
[Authorize(Roles = $"{nameof(UserRole.Lecturer)},{nameof(UserRole.Admin)}")]
public class DownloadModel(IDocumentService documentService) : PageModel
{
    private readonly IDocumentService _documentService = documentService;

    /// <summary>
    /// Downloads the original document file.
    /// </summary>
    /// <param name="id">The document identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The document file or a not-found result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cxlTkn)
    {
        var doc = await _documentService.GetByIdAsync(id, null!, cxlTkn);

        if (doc == null)
            return NotFound();

        return File(
            fileStream: System.IO.File.OpenRead(doc.FilePath),
            contentType: doc.ContentType,
            fileDownloadName: doc.OriginalFileName);
    }
}
