using AutoMapper;
using Domain.Contracts;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Extensions;
using Presentation.ViewModels;

namespace Presentation.Pages.Chat;

/// <summary>
/// Displays the chat workspace and validates access to an optional active session.
/// </summary>
[Authorize]
public class IndexModel(
    IChatPersistenceService chatPersistenceService,
    ISubjectService subjectService,
    IMapper mapper) : PageModel
{
    private readonly IChatPersistenceService _chatPersistenceService = chatPersistenceService;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets the chat page view model rendered by the page.
    /// </summary>
    public ChatPageVm ViewModel { get; private set; } = new();

    /// <summary>
    /// Loads the chat page for the current user and optional session.
    /// </summary>
    /// <param name="id">Optional active chat session identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The chat page, unauthorized, or not found result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cxlTkn)
    {
        Guid userId;
        try
        {
            userId = User.GetUserId();
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }

        var subjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cxlTkn);

        if (id.HasValue)
        {
            var session = await _chatPersistenceService.GetSessionByIdAsync(id.Value, cxlTkn);

            if (session == null || session.UserId != userId)
            {
                return NotFound();
            }
        }

        ViewModel = new ChatPageVm
        {
            ActiveSessionId = id,
        };

        return Page();
    }
}
