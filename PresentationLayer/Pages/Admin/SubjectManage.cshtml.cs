using Domain.Contracts;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Presentation.RealtimeWeb;
using Presentation.ViewModels;

namespace Presentation.Pages.Admin;

/// <summary>
/// Displays the administrator subject management page with filters and pagination.
/// Handles page display and AJAX subject/chapter/member management endpoints.
/// </summary>
[Authorize(Roles = "Admin")]
public class SubjectManageModel(
    ISubjectService subjectService,
    IHubContext<ResourceHub, IResourceClient> hub
    ) : PageModel
{
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IHubContext<ResourceHub, IResourceClient> _hub = hub;

    /// <summary>
    /// Gets the subject management view model rendered by the page.
    /// </summary>
    public AdminSubjectListVm ViewModel { get; private set; } = new();

    [FromHeader]
    public string CallerSignalRConnectionId { get; set; } = string.Empty;

    private static List<string> OtherSubjectGroups(int subjectId, Guid[] userIds, Guid[] docIds)
    {
        var groups = NotificationTargets.Subject(subjectId, userIds, docIds).ToList();
        groups.Remove(ThisGroup());
        return groups;
    }

    private static List<string> OtherChapterGroups(int chapterId, Guid[] docIds)
    {
        var groups = NotificationTargets.Chapter(chapterId, docIds).ToList();
        groups.Remove(ThisGroup());
        return groups;
    }

    private static List<string> OtherMembershipGroups(int subjectId, Guid userId, Guid[] docIds)
    {
        var groups = NotificationTargets.Membership(subjectId, userId, docIds).ToList();
        groups.Remove(ThisGroup());
        return groups;
    }

    private static string ThisGroup() => HubGroups.Resource(PageTypes.SubjectManage);

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

            var upd = new ResourceUpdate
            {
                ResourceType = "subject",
                Action = "created",
                ResourceId = subject.Id.ToString(),
                ResourceName = subject.Name,
            };
            await _hub.Clients.Groups(OtherSubjectGroups).ResourceChanged(upd);
            await _hub.Clients.GroupExcept(ThisGroup, CallerSignalRConnectionId).ResourceChanged(upd);

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
    public async Task<IActionResult> OnPutUpdateSubjectAsync([FromQuery] int id, [FromBody] AdminUpdateSubjectVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });
        if (id != vm.Id)
            return BadRequest(new { success = false, error = "Subject ID mismatch." });

        try
        {
            var subject = await _subjectService.UpdateSubjectAsync(vm.Id, vm.SubjectCode, vm.SubjectName, vm.Description);

            var upd = new ResourceUpdate
            {
                ResourceType = "subject",
                Action = "updated",
                ResourceId = subject.Id.ToString(),
                ResourceName = subject.Name,
            };
            await _hub.Clients.Groups(OtherSubjectGroups).ResourceChanged(upd);
            await _hub.Clients.GroupExcept(ThisGroup, CallerSignalRConnectionId).ResourceChanged(upd);

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
    public async Task<IActionResult> OnDeleteDeleteSubjectAsync([FromQuery] int id)
    {
        try
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id);
            if (subject == null) return NotFound();
            await _subjectService.DeleteSubjectAsync(id);

            var upd = new ResourceUpdate
            {
                ResourceType = "subject",
                Action = "deleted",
                ResourceId = id.ToString(),
                ResourceName = subject.Name,
            };
            await _hub.Clients.Groups(OtherSubjectGroups).ResourceChanged(upd);
            await _hub.Clients.GroupExcept(ThisGroup, CallerSignalRConnectionId).ResourceChanged(upd);

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

            var upd = new ResourceUpdate
            {
                ResourceType = "chapter",
                Action = "created",
                ResourceId = chapter.Id.ToString(),
                AlternateResourceId = [chapter.SubjectId.ToString(), chapter.ChapterNumber.ToString() ?? string.Empty],
                ResourceName = chapter.Name,
            };
            await _hub.Clients.Groups(OtherChapterGroups).ResourceChanged(upd);
            await _hub.Clients.GroupExcept(ThisGroup, CallerSignalRConnectionId).ResourceChanged(upd);

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
    public async Task<IActionResult> OnPutUpdateChapterAsync([FromQuery] int id, [FromBody] AdminUpdateChapterVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, error = "Invalid input data." });
        if (id != vm.Id)
            return BadRequest(new { success = false, error = "Chapter ID mismatch." });

        try
        {
            var chapter = await _subjectService.UpdateChapterAsync(vm.Id, vm.ChapterName, vm.ChapterNumber);

            var upd = new ResourceUpdate
            {
                ResourceType = "chapter",
                Action = "updated",
                ResourceId = chapter.Id.ToString(),
                AlternateResourceId = [chapter.SubjectId.ToString(), chapter.ChapterNumber.ToString() ?? string.Empty],
                ResourceName = chapter.Name,
            };
            await _hub.Clients.Groups(OtherChapterGroups).ResourceChanged(upd);
            await _hub.Clients.GroupExcept(ThisGroup, CallerSignalRConnectionId).ResourceChanged(upd);

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
    public async Task<IActionResult> OnDeleteDeleteChapterAsync([FromQuery] int id)
    {
        try
        {
            var chapter = await _subjectService.GetChapterByIdAsync(id);
            if (chapter == null) return NotFound();

            await _subjectService.DeleteChapterAsync(id);

            var upd = new ResourceUpdate
            {
                ResourceType = "chapter",
                Action = "deleted",
                ResourceId = id.ToString(),
                ResourceName = chapter.Name,
                Properties =
                {
                    { nameof(Chapter.SubjectId), chapter.SubjectId.ToString() },
                },
            };
            await _hub.Clients.Groups(OtherChapterGroups).ResourceChanged(upd);
            await _hub.Clients.GroupExcept(ThisGroup, CallerSignalRConnectionId).ResourceChanged(upd);

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
            var membership = await _subjectService.AssignMemberAsync(subjectId, vm.UserId, membershipRole);

            var upd = new ResourceUpdate
            {
                ResourceType = ResourceTypes.Membership,
                Action = Actions.Created,
                ResourceId = membership.Id.ToString(),
                ResourceName = $"{membership.Subject.Name} <=> {membership.User.FullName}",
                Properties =
                {
                    { nameof(SubjectMembership.SubjectId), membership.SubjectId.ToString() },
                    { nameof(SubjectMembership.UserId), membership.UserId.ToString() },
                },
            };

            var docsInSubject = /* WHAT NOW? */;

            await _hub.Clients.Groups(
                OtherMembershipGroups(membership.SubjectId, membership.UserId, docsInSubject))
                .ResourceChanged(upd);

            await _hub.Clients
                .GroupExcept(ThisGroup(), CallerSignalRConnectionId)
                .ResourceChanged(upd);

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
    public async Task<IActionResult> OnDeleteRemoveMemberAsync([FromQuery] int subjectId, [FromQuery] Guid userId)
    {
        try
        {
            var membership = await _subjectService.RemoveMemberAsync(subjectId, userId);

            var upd = new ResourceUpdate
            {
                ResourceType = "subject_membership",
                Action = "deleted",
                ResourceId = membership.Id.ToString(),
                ResourceName = $"{membership.Subject.Name} <=> {membership.User.FullName}",
                Properties =
                {
                    { nameof(SubjectMembership.SubjectId), membership.SubjectId.ToString() },
                    { nameof(SubjectMembership.UserId), membership.UserId.ToString() },
                },
            };
            await _hub.Clients.Groups(OtherMembershipGroups()).ResourceChanged(upd);
            await _hub.Clients.GroupExcept(ThisGroup, CallerSignalRConnectionId).ResourceChanged(upd);

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"Error removing member: {ex.Message}" });
        }
    }
}
