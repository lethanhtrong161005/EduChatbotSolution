
using Domain.Contracts; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PresentationLayer.Controllers
{
    [Authorize]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var document = await _documentService.GetDocumentWithCommentsAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            return View(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(Guid documentId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return RedirectToAction(nameof(Details), new { id = documentId });
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdString, out Guid userId))
            {
                await _documentService.AddCommentAsync(documentId, userId, content);
            }

            return RedirectToAction(nameof(Details), new { id = documentId });
        }
    }
}