using Domain.Contracts;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Extensions;
using System.Security.Claims;

namespace Presentation.Pages.Account;

/// <summary>
/// Handles rendering and logic for the user profile page.
/// Exposes tabs for User Info, Subject Memberships, and Uploaded Documents.
/// </summary>
[Authorize]
public class ProfileModel(
    UserManager<ApplicationUser> userManager,
    ISubjectService subjectService,
    IDocumentService documentService)
    : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IDocumentService _documentService = documentService;

    /// <summary>Gets the current authenticated user's details.</summary>
    public ApplicationUser UserDetails { get; private set; } = null!;

    /// <summary>Gets the user's primary role.</summary>
    public string Role { get; private set; } = "Student";

    /// <summary>Gets the list of subject memberships assigned to this user.</summary>
    public List<SubjectMembership> Memberships { get; private set; } = [];

    /// <summary>Gets the documents uploaded by this user, along with assignment warnings.</summary>
    public List<MyDocVm> MyDocuments { get; private set; } = [];

    /// <summary>
    /// View model for displaying documents in the Profile page.
    /// </summary>
    public class MyDocVm
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public DocumentType FileType { get; set; }
        public long? FileSize { get; set; }
        public DocumentStatus Status { get; set; }
        public DateTime UploadedAt { get; set; }
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public bool IsNoLongerAssigned { get; set; }
    }

    /// <summary>
    /// Loads the profile data on GET.
    /// </summary>
    /// <param name="cxlTkn">A token to cancel the operation.</param>
    public async Task<IActionResult> OnGetAsync(CancellationToken cxlTkn)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdString, out var userId))
        {
            return Challenge();
        }

        var user = await _userManager.FindByIdAsync(userIdString);
        if (user == null)
        {
            return NotFound("User not found.");
        }
        UserDetails = user;

        var roles = await _userManager.GetRolesAsync(user);
        Role = roles.FirstOrDefault() ?? "Student";

        // Load active memberships for the user, including the parent Subject
        var memberships = await _subjectService.GetMembershipsOfUserAsync(userId, cxlTkn);
        Memberships = [.. memberships.OrderBy(m => m.Subject.Code)];

        // Load all documents uploaded by this user, including their Chapter and parent Subject
        var documents = await _documentService.GetByUploaderAsync(userId, cxlTkn);

        var activeSubjectIds = Memberships.Select(m => m.SubjectId).ToHashSet();

        // Map documents and perform the review check: is the uploader still assigned to the subject?
        MyDocuments = [.. documents.Select(d => new MyDocVm
        {
            Id = d.Id,
            Title = d.Title,
            OriginalFileName = d.OriginalFileName,
            FileType = d.FileType,
            FileSize = d.FileSize,
            Status = d.Status,
            UploadedAt = d.UploadedAt,
            SubjectCode = d.Chapter.Subject.Code,
            SubjectName = d.Chapter.Subject.Name,
            ChapterName = d.Chapter.Name,
            IsNoLongerAssigned = !activeSubjectIds.Contains(d.Chapter.SubjectId)
        }).OrderByDescending(d => d.UploadedAt)];

        return Page();
    }

    public async Task<IActionResult> OnGetGetProfileAsync()
    {
        try
        {
            var userId = User.GetUserId();
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                return NotFound("User not found.");

            return new JsonResult(user);
        }
        catch (UserClaimException)
        {
            return Challenge();
        }
    }

    public async Task<IActionResult> OnGetGetMembershipsAsync(CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();
            var memberships = (await _subjectService.GetMembershipsOfUserAsync(userId, cxlTkn))
                .OrderBy(m => m.Subject.Code)
                .ToList();

            memberships.ForEach(e => e.Subject.Memberships = []);

            var dto = memberships.Select(e => new
            {
                e.Id,
                e.UserId,
                SubjectId = e.SubjectId.ToString(),
                SubjectCode = e.Subject.Code,
                SubjectName = e.Subject.Name,
                e.Role,
                e.AssignedAt,
                e.CreatedAt,
                e.UpdatedAt,
            });

            return new JsonResult(dto);
        }
        catch (UserClaimException)
        {
            return Challenge();
        }
    }

    public async Task<IActionResult> OnGetGetDocumentsAsync(CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();

            var documents = (await _documentService.GetByUploaderAsync(userId, cxlTkn)).ToList();

            var memberships = await _subjectService.GetMembershipsOfUserAsync(userId, cxlTkn);
            var activeSubjectIds = memberships.Select(m => m.SubjectId).ToHashSet();

            var dto = documents.Select(e => new
            {
                e.Id,
                e.Title,
                e.Description,
                SubjectId = e.Chapter.SubjectId.ToString(),
                SubjectCode = e.Chapter.Subject.Code,
                ChapterName = e.Chapter.Name,
                e.FileName,
                FileType = e.FileType.ToString(),
                e.FileSize,
                e.UploadedAt,
                e.Status,
                IsNoLongerAssigned = !activeSubjectIds.Contains(e.Chapter.SubjectId),
            }).OrderByDescending(d => d.UploadedAt);

            return new JsonResult(dto);
        }
        catch (UserClaimException)
        {
            return Challenge();
        }
    }
}
