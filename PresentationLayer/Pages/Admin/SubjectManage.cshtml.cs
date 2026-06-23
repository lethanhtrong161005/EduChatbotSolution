using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.ViewModels;

namespace Presentation.Pages.Admin;

/// <summary>
/// Displays the administrator subject management page with filters and pagination.
/// Handles page display and AJAX subject/chapter/member management endpoints.
/// </summary>
[Authorize(Roles = "Admin")]
public class SubjectManageModel(ISubjectService subjectService) : PageModel
{
    private readonly ISubjectService _subjectService = subjectService;

    /// <summary>
    /// Gets the subject management view model rendered by the page.
    /// </summary>
    public AdminSubjectListVm ViewModel { get; private set; } = new();

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

    /// <summary>
    /// Creates a new subject.
    /// </summary>
    public async Task<IActionResult> OnPostCreateSubjectAsync([FromBody] AdminCreateSubjectVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });

        try
        {
            var subject = await _subjectService.CreateSubjectAsync(vm.SubjectCode, vm.SubjectName, vm.Description);
            return new JsonResult(new { success = true, subject });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Updates a subject.
    /// </summary>
    public async Task<IActionResult> OnPostUpdateSubjectAsync([FromQuery] int id, [FromBody] AdminUpdateSubjectVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });
        if (id != vm.Id)
            return BadRequest(new { success = false, error = "Subject ID mismatch." });

        try
        {
            var subject = await _subjectService.UpdateSubjectAsync(vm.Id, vm.SubjectCode, vm.SubjectName, vm.Description);
            return new JsonResult(new { success = true, subject });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Deletes a subject.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteSubjectAsync([FromQuery] int id)
    {
        try
        {
            await _subjectService.DeleteSubjectAsync(id);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"Error deleting subject: {ex.Message}" });
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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Creates a chapter.
    /// </summary>
    public async Task<IActionResult> OnPostCreateChapterAsync([FromBody] AdminCreateChapterVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });

        try
        {
            var chapter = await _subjectService.CreateChapterAsync(vm.SubjectId, vm.ChapterName, vm.ChapterNumber);
            return new JsonResult(new { success = true, chapter });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Updates a chapter.
    /// </summary>
    public async Task<IActionResult> OnPostUpdateChapterAsync([FromQuery] int id, [FromBody] AdminUpdateChapterVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });
        if (id != vm.Id)
            return BadRequest(new { success = false, error = "Chapter ID mismatch." });

        try
        {
            var chapter = await _subjectService.UpdateChapterAsync(vm.Id, vm.ChapterName, vm.ChapterNumber);
            return new JsonResult(new { success = true, chapter });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Deletes a chapter.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteChapterAsync([FromQuery] int id)
    {
        try
        {
            await _subjectService.DeleteChapterAsync(id);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"Error deleting chapter: {ex.Message}" });
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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets users eligible for assignment to a subject in a specific role.
    /// </summary>
    public async Task<IActionResult> OnGetGetEligibleUsersAsync([FromQuery] int subjectId, [FromQuery] string role, [FromQuery] string? search)
    {
        if (!Enum.TryParse<MembershipRole>(role, true, out var membershipRole))
            return BadRequest(new { success = false, error = "Invalid assignment role." });

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
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Assigns a user to a subject in a specific role.
    /// </summary>
    public async Task<IActionResult> OnPostAssignMemberAsync([FromQuery] int subjectId, [FromBody] AdminAssignMemberVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });
        if (!Enum.TryParse<MembershipRole>(vm.Role, true, out var membershipRole))
            return BadRequest(new { success = false, error = "Invalid assignment role." });

        try
        {
            await _subjectService.AssignMemberAsync(subjectId, vm.UserId, membershipRole);
            return new JsonResult(new { success = true });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return new JsonResult(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    /// <summary>
    /// Removes a user from a subject.
    /// </summary>
    public async Task<IActionResult> OnPostRemoveMemberAsync([FromQuery] int subjectId, [FromQuery] Guid userId)
    {
        try
        {
            await _subjectService.RemoveMemberAsync(subjectId, userId);
            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"Error removing member: {ex.Message}" });
        }
    }
}
