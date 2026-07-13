using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Presentation.ViewModels;

namespace Presentation.Pages.Admin;

/// <summary>
/// Displays the administrator subject management page with filters and pagination.
/// Handles page display and AJAX subject/chapter/member management endpoints.
/// </summary>
[Authorize(Roles = "Admin")]
public class SubjectManageModel(
    ISubjectService subjectService,
    IResourceRealtimeNotifier notifier)
    : PageModel
{
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IResourceRealtimeNotifier _notifier = notifier;

    /// <summary>
    /// Gets the subject management view model rendered by the page.
    /// </summary>
    public AdminSubjectListVm ViewModel { get; private set; } = new();

    [FromHeader]
    public string CallerConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Loads subjects for the subject management page.
    /// </summary>
    /// <param name="code">Optional subject code filter.</param>
    /// <param name="name">Optional subject name filter.</param>
    /// <param name="limit">The requested page size.</param>
    /// <param name="offset">The zero-based record offset.</param>
    /// <returns>A task that renders the page.</returns>
    public async Task OnGetAsync(string? code, string? name, int limit = 10, int offset = 0)
    {
        var subjects = await _subjectService.GetPagedSubjectsAsync(code, name, limit, offset);

        ViewModel = new AdminSubjectListVm
        {
            CodeFilter = code,
            NameFilter = name,
            Limit = limit,
            Offset = offset,
            Subjects = subjects,
        };
    }

    public async Task<IActionResult> OnGetGetSubjectsAsync(string? code, string? name, int limit = 10, int offset = 0)
    {
        try
        {
            var subjects = await _subjectService.GetPagedSubjectsAsync(code, name, limit, offset);
            return new JsonResult(subjects);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Creates a new subject.
    /// </summary>
    public async Task<IActionResult> OnPostCreateSubjectAsync([FromBody] AdminCreateSubjectVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Success = false, Error = "Invalid input data." });

        try
        {
            var subject = await _subjectService.CreateSubjectAsync(vm.SubjectCode, vm.SubjectName, vm.Description);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Subject,
                Action = ResourceAction.Created,
                ResourceId = subject.Id.ToString(),
                ResourceName = subject.Name,
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            return new JsonResult(new { Success = true, subject });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Updates a subject.
    /// </summary>
    public async Task<IActionResult> OnPutUpdateSubjectAsync([FromQuery] int id, [FromBody] AdminUpdateSubjectVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Success = false, Error = "Invalid input data." });
        if (id != vm.Id)
            return BadRequest(new { Success = false, Error = "Subject ID mismatch." });

        try
        {
            var subject = await _subjectService.UpdateSubjectAsync(vm.Id, vm.SubjectCode, vm.SubjectName, vm.Description);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Subject,
                Action = ResourceAction.Updated,
                ResourceId = subject.Id.ToString(),
                ResourceName = subject.Name,
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            return new JsonResult(new { Success = true, subject });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Deletes a subject.
    /// </summary>
    public async Task<IActionResult> OnDeleteDeleteSubjectAsync([FromQuery] int id)
    {
        try
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null) return NotFound();
            await _subjectService.DeleteSubjectAsync(id);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Subject,
                Action = ResourceAction.Deleted,
                ResourceId = id.ToString(),
                ResourceName = subject.Name,
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            return new JsonResult(new { Success = true });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Gets chapters for a subject.
    /// </summary>
    public async Task<IActionResult> OnGetGetChaptersAsync([FromQuery] int subjectId)
    {
        try
        {
            var chapters = await _subjectService.GetChaptersBySubjectIdAsync(subjectId);
            return new JsonResult(chapters.Select(c => new { c.Id, c.Name, c.ChapterNumber }));
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Creates a chapter.
    /// </summary>
    public async Task<IActionResult> OnPostCreateChapterAsync([FromBody] AdminCreateChapterVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Success = false, Error = "Invalid input data." });

        try
        {
            var chapter = await _subjectService.CreateChapterAsync(vm.SubjectId, vm.ChapterName, vm.ChapterNumber);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Chapter,
                Action = ResourceAction.Created,
                ResourceId = chapter.Id.ToString(),
                ResourceName = chapter.Name,
                Properties =
                {
                    { nameof(Chapter.SubjectId), chapter.SubjectId.ToString() },
                    { nameof(Chapter.ChapterNumber), chapter.ChapterNumber.ToString() },
                },
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            return new JsonResult(new { Success = true, chapter });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Updates a chapter.
    /// </summary>
    public async Task<IActionResult> OnPutUpdateChapterAsync([FromQuery] int id, [FromBody] AdminUpdateChapterVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Success = false, Error = "Invalid input data." });
        if (id != vm.Id)
            return BadRequest(new { Success = false, Error = "Chapter ID mismatch." });

        try
        {
            var chapter = await _subjectService.UpdateChapterAsync(vm.Id, vm.ChapterName, vm.ChapterNumber);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Chapter,
                Action = ResourceAction.Updated,
                ResourceId = chapter.Id.ToString(),
                ResourceName = chapter.Name,
                Properties =
                {
                    { nameof(Chapter.SubjectId), chapter.SubjectId.ToString() },
                    { nameof(Chapter.ChapterNumber), chapter.ChapterNumber.ToString() },
                },
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            return new JsonResult(new { Success = true, chapter });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Deletes a chapter.
    /// </summary>
    public async Task<IActionResult> OnDeleteDeleteChapterAsync([FromQuery] int id)
    {
        try
        {
            var chapter = await _subjectService.GetChapterByIdAsync(id);
            if (chapter == null) return NotFound();

            await _subjectService.DeleteChapterAsync(id);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Chapter,
                Action = ResourceAction.Deleted,
                ResourceId = id.ToString(),
                ResourceName = chapter.Name,
                Properties =
                {
                    { nameof(Chapter.SubjectId), chapter.SubjectId.ToString() },
                    { nameof(Chapter.ChapterNumber), chapter.ChapterNumber.ToString() },
                },
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            return new JsonResult(new { Success = true });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Gets members assigned to a subject.
    /// </summary>
    public async Task<IActionResult> OnGetGetMembersAsync([FromQuery] int subjectId)
    {
        try
        {
            var members = await _subjectService.GetMembershipsBySubjectIdAsync(subjectId);
            var list = members.Select(m => new SubjectMemberItemDto(
                m.UserId,
                m.User.FullName,
                m.User.Email ?? string.Empty,
                m.Role.ToString(),
                m.AssignedAt
            ));
            return new JsonResult(list);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Gets users eligible for assignment to a subject in a specific role.
    /// </summary>
    public async Task<IActionResult> OnGetGetEligibleUsersAsync([FromQuery] int subjectId, [FromQuery] string role, [FromQuery] string? search)
    {
        if (!Enum.TryParse<MembershipRole>(role, true, out var membershipRole))
            return BadRequest(new { Success = false, Error = "Invalid assignment role." });

        try
        {
            var users = await _subjectService.GetEligibleUsersForAssignmentAsync(subjectId, membershipRole, search);
            var list = users.Select(u => new EligibleUserItemDto(
                u.Id,
                u.FullName,
                u.Email ?? string.Empty
            ));
            return new JsonResult(list);
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Assigns a user to a subject in a specific role.
    /// </summary>
    public async Task<IActionResult> OnPostAssignMemberAsync([FromQuery] int subjectId, [FromBody] AdminAssignMemberVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Success = false, Error = "Invalid input data." });
        if (!Enum.TryParse<MembershipRole>(vm.Role, true, out var membershipRole))
            return BadRequest(new { Success = false, Error = "Invalid assignment role." });

        try
        {
            var membership = await _subjectService.AssignMemberAsync(subjectId, vm.UserId, membershipRole);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Membership,
                Action = ResourceAction.Created,
                ResourceId = membership.Id.ToString(),
                ResourceName = $"{membership.Subject.Name} <=> {membership.User.FullName}",
                Properties =
                {
                    { nameof(Membership.SubjectId), membership.SubjectId.ToString() },
                    { nameof(Membership.UserId), membership.UserId.ToString() },
                },
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            return new JsonResult(new { Success = true });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }

    /// <summary>
    /// Removes a user from a subject.
    /// </summary>
    public async Task<IActionResult> OnDeleteRemoveMemberAsync([FromQuery] int subjectId, [FromQuery] Guid userId)
    {
        try
        {
            var membership = await _subjectService.RemoveMemberAsync(subjectId, userId);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.Membership,
                Action = ResourceAction.Deleted,
                ResourceId = membership.Id.ToString(),
                ResourceName = $"{membership.Subject.Name} <=> {membership.User.FullName}",
                Properties =
                {
                    { nameof(Membership.SubjectId), membership.SubjectId.ToString() },
                    { nameof(Membership.UserId), membership.UserId.ToString() },
                },
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            return new JsonResult(new { Success = true });
        }
        catch (BadRequestException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}" });
        }
        catch (EntityNotFoundException ex)
        {
            return NotFound(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status404NotFound)}: {ex.Message}" });
        }
        catch (EntityValidationException ex)
        {
            return BadRequest(new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status400BadRequest)}: {ex.Message}", ex.Property });
        }
        catch (EntityConflictException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status409Conflict)}: {ex.Message}", ex.Property });
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { Error = $"{ReasonPhrases.GetReasonPhrase(StatusCodes.Status500InternalServerError)}" });
        }
    }
}
