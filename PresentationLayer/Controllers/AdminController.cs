using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

/// <summary>
/// Handles all admin-panel pages and AJAX endpoints for user management.
/// All actions are restricted to the <c>Admin</c> role.
/// </summary>
[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController(
    IUserManagementService userManagementService,
    UserManager<ApplicationUser> userManager,
    ISubjectService subjectService) : Controller
{
    private readonly IUserManagementService _userMgmt = userManagementService;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ISubjectService _subjectService = subjectService;

    // ── USER MANAGE PAGE ──────────────────────────────────────────

    /// <summary>
    /// Displays the user management page with a paginated, filtered list of users.
    /// </summary>
    /// <param name="name">Optional name filter.</param>
    /// <param name="email">Optional email filter.</param>
    /// <param name="role">Optional role filter.</param>
    /// <param name="limit">Page size (default 10).</param>
    /// <param name="offset">Record offset (default 0).</param>
    [HttpGet("user-manage")]
    public async Task<IActionResult> UserManage(
        string? name, string? email, string? role, int limit = 10, int offset = 0)
    {
        var users = await _userMgmt.GetPagedUsersAsync(name, email, role, limit, offset);
        var roles = await _userMgmt.GetAllRolesAsync();

        var vm = new AdminUserListVm
        {
            NameFilter = name,
            EmailFilter = email,
            RoleFilter = role,
            Limit = limit,
            Offset = offset,
            Users = users,
            AvailableRoles = roles,
        };
        return View(vm);
    }

    // ── ROLES API ─────────────────────────────────────────────────

    /// <summary>
    /// Returns the list of available roles as JSON for populating the role dropdown.
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _userMgmt.GetAllRolesAsync();
        return Json(roles);
    }

    // ── CREATE ────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new active user account and emails the initial login credentials.
    /// Returns JSON so the page can show inline success/error without a full reload.
    /// </summary>
    /// <param name="vm">Create-user form data submitted via AJAX.</param>
    [HttpPost("users/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid form data." });

        var (success, error) = await _userMgmt.CreateUserAsync(
            new Domain.Contracts.CreateUserDto(vm.FullName, vm.Email, vm.Password, vm.Role));

        return Json(new { success, error });
    }

    // ── UPDATE ────────────────────────────────────────────────────

    /// <summary>
    /// Updates a user's profile (name, email, role). Applies optimistic-concurrency protection
    /// using the <c>updatedAt</c> token sent from the client.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="vm">Update form data.</param>
    [HttpPut("users/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] AdminUpdateUserVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid form data." });

        if (id != vm.UserId)
            return BadRequest(new { success = false, error = "User ID mismatch." });

        var (success, error) = await _userMgmt.UpdateUserAsync(
            new Domain.Contracts.UpdateUserDto(vm.UserId, vm.FullName, vm.Email, vm.Role, vm.UpdatedAt));

        if (success)
        {
            var message = vm.Email != vm.OriginalEmail
                ? "Changes saved! A verification email has been sent to the new email address."
                : "Changes saved successfully!";
            return Json(new { success, message });
        }

        return Json(new { success, error });
    }

    // ── SOFT DELETE ───────────────────────────────────────────────

    /// <summary>
    /// Soft-deletes a user account. Sends a deletion-notification email.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    [HttpDelete("users/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var (success, error) = await _userMgmt.SoftDeleteUserAsync(id);
        return Json(new { success, error });
    }

    // ── DISABLE ───────────────────────────────────────────────────

    /// <summary>
    /// Disables a user account using optimistic-concurrency protection.
    /// Sends a disable-notification email on success.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="body">Body containing the <c>updatedAt</c> concurrency token.</param>
    [HttpPost("users/{id:guid}/disable")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableUser(Guid id, [FromBody] ConcurrencyTokenBody body)
    {
        var (success, error) = await _userMgmt.DisableUserAsync(id, body.UpdatedAt);
        return Json(new { success, error });
    }

    // ── REACTIVATE ────────────────────────────────────────────────

    /// <summary>
    /// Re-enables a previously disabled user account using optimistic-concurrency protection.
    /// </summary>
    /// <param name="id">The user's ID.</param>
    /// <param name="body">Body containing the <c>updatedAt</c> concurrency token.</param>
    [HttpPost("users/{id:guid}/reactivate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReactivateUser(Guid id, [FromBody] ConcurrencyTokenBody body)
    {
        var (success, error) = await _userMgmt.ReactivateUserAsync(id, body.UpdatedAt);
        return Json(new { success, error });
    }

    // ── SUBJECTS MANAGEMENT ───────────────────────────────────────

    /// <summary>
    /// Displays the subject management page with a paginated, filtered list of subjects.
    /// </summary>
    [HttpGet("subject-manage")]
    public async Task<IActionResult> SubjectManage(
        string? code, string? name, int limit = 10, int offset = 0)
    {
        var subjects = await _subjectService.GetPagedSubjectsAsync(code, name, limit, offset);

        var vm = new AdminSubjectListVm
        {
            CodeFilter = code,
            NameFilter = name,
            Limit = limit,
            Offset = offset,
            Subjects = subjects
        };
        return View(vm);
    }

    [HttpPost("subjects/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSubject([FromBody] AdminCreateSubjectVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });

        try
        {
            var subject = await _subjectService.CreateSubjectAsync(vm.SubjectCode, vm.SubjectName, vm.Description);
            return Json(new { success = true, subject });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    [HttpPut("subjects/update/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] AdminUpdateSubjectVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });
        if (id != vm.Id)
            return BadRequest(new { success = false, error = "Subject ID mismatch." });

        try
        {
            var subject = await _subjectService.UpdateSubjectAsync(vm.Id, vm.SubjectCode, vm.SubjectName, vm.Description);
            return Json(new { success = true, subject });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    [HttpDelete("subjects/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        try
        {
            await _subjectService.DeleteSubjectAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = $"Error deleting subject: {ex.Message}" });
        }
    }

    // ── CHAPTERS MANAGEMENT ───────────────────────────────────────

    [HttpGet("subjects/{id:int}/chapters")]
    public async Task<IActionResult> GetChapters(int id)
    {
        try
        {
            var chapters = await _subjectService.GetChaptersBySubjectIdAsync(id);
            return Json(chapters.Select(c => new { c.Id, c.Name, c.ChapterNumber }));
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("chapters/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateChapter([FromBody] AdminCreateChapterVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });

        try
        {
            var chapter = await _subjectService.CreateChapterAsync(vm.SubjectId, vm.ChapterName, vm.ChapterNumber);
            return Json(new { success = true, chapter });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    [HttpPut("chapters/update/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateChapter(int id, [FromBody] AdminUpdateChapterVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });
        if (id != vm.Id)
            return BadRequest(new { success = false, error = "Chapter ID mismatch." });

        try
        {
            var chapter = await _subjectService.UpdateChapterAsync(vm.Id, vm.ChapterName, vm.ChapterNumber);
            return Json(new { success = true, chapter });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    [HttpDelete("chapters/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteChapter(int id)
    {
        try
        {
            await _subjectService.DeleteChapterAsync(id);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = $"Error deleting chapter: {ex.Message}" });
        }
    }

    // ── MEMBERS MANAGEMENT ────────────────────────────────────────

    [HttpGet("subjects/{id:int}/members")]
    public async Task<IActionResult> GetMembers(int id)
    {
        try
        {
            var members = await _subjectService.GetMembershipsBySubjectIdAsync(id);
            var list = members.Select(m => new SubjectMemberItemDto(
                m.UserId,
                m.User.FullName,
                m.User.Email ?? string.Empty,
                m.Role.ToString(),
                m.AssignedAt
            ));
            return Json(list);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("subjects/{id:int}/eligible-users")]
    public async Task<IActionResult> GetEligibleUsers(int id, string role, string? search)
    {
        if (!Enum.TryParse<MembershipRole>(role, true, out var membershipRole))
            return BadRequest(new { success = false, error = "Invalid assignment role." });

        try
        {
            var users = await _subjectService.GetEligibleUsersForAssignmentAsync(id, membershipRole, search);
            var list = users.Select(u => new EligibleUserItemDto(
                u.Id,
                u.FullName,
                u.Email ?? string.Empty
            ));
            return Json(list);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("subjects/{id:int}/members/assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignMember(int id, [FromBody] AdminAssignMemberVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });
        if (!Enum.TryParse<MembershipRole>(vm.Role, true, out var membershipRole))
            return BadRequest(new { success = false, error = "Invalid assignment role." });

        try
        {
            await _subjectService.AssignMemberAsync(id, vm.UserId, membershipRole);
            return Json(new { success = true });
        }
        catch (Domain.Exceptions.BadRequestException ex)
        {
            return Json(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = $"System error: {ex.Message}" });
        }
    }

    [HttpDelete("subjects/{id:int}/members/remove/{userId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int id, Guid userId)
    {
        try
        {
            await _subjectService.RemoveMemberAsync(id, userId);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, error = $"Error removing member: {ex.Message}" });
        }
    }
}

/// <summary>Minimal request body carrying only the optimistic-concurrency token.</summary>
public record ConcurrencyTokenBody(DateTimeOffset UpdatedAt);
