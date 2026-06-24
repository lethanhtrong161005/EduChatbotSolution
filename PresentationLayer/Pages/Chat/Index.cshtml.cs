using AutoMapper;
using Domain.Contracts;
using Domain.Exceptions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Presentation.Constants;
using Presentation.DTOs;
using Presentation.Extensions;
using Presentation.ViewModels;

namespace Presentation.Pages.Chat;

/// <summary>
/// Displays the chat workspace and validates access to an optional active session.
/// Handles chat API endpoints via handlers.
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

    /// <summary>
    /// Returns the list of subjects accessible to the current user.
    /// </summary>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnGetGetSubjectHeadersAsync(CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();
            var subjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cxlTkn);
            var res = _mapper.Map<List<SubjectHeaderDto>>(subjects);
            return new JsonResult(res);
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Returns the list of chat session headers for the current user.
    /// </summary>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnGetGetSessionHeadersAsync(CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();
            var sessions = await _chatPersistenceService.GetSessionHeadersByUserAsync(userId, cxlTkn);
            var res = _mapper.Map<List<SessionHeaderDto>>(sessions);
            return new JsonResult(res);
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Returns a specific chat session with all messages for the current user.
    /// </summary>
    /// <param name="id">The session identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnGetGetSessionAsync([FromQuery] Guid id, CancellationToken cxlTkn)
    {
        var session = await _chatPersistenceService.GetSessionWithMessagesByIdAsync(id, cxlTkn);

        if (session == null || session.UserId != User.GetUserId())
            return NotFound();

        var dto = _mapper.Map<ChatSessionDto>(session);
        return new JsonResult(dto);
    }

    /// <summary>
    /// Creates a new chat session.
    /// </summary>
    /// <param name="req">Session creation request.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnPostCreateSessionAsync([FromForm] CreateChatSessionRequest req, CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();

            if (!req.SubjectId.HasValue)
            {
                var subjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cxlTkn);
                if (!subjects.Any())
                    return BadRequest();
            }

            if (req.SubjectId.HasValue
                && !await _subjectService.HasAccessAsync(req.SubjectId.Value, userId, cxlTkn))
            {
                return BadRequest();
            }

            var session = await _chatPersistenceService.CreateSessionAsync(
                userId,
                req.SubjectId,
                $"Session {DateTime.UtcNow:f}",
                cxlTkn);

            var res = _mapper.Map<CreateChatSessionResponse>(session);
            return new JsonResult(res);
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Generates the next AI message in a chat session.
    /// </summary>
    /// <param name="req">Message generation request.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnPostGenerateAsync([FromForm] GenerateChatRequest req, CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();
            var session = await _chatPersistenceService.GetSessionByIdAsync(req.SessionId, cxlTkn);

            if (session == null || session.UserId != userId)
                return NotFound();

            var userMessage = await _chatPersistenceService.CreateUserMessageAsync(req.SessionId, req.Content, cxlTkn);
            var assistantMessage = await _chatPersistenceService.CreateStreamingAssistantMessageAsync(req.SessionId, cxlTkn);

            BackgroundJob.Enqueue<IChatGenerationCoordinator>(
                HangfireConstants.HighPriorityQueue,
                e => e.GenerateAsync(req.SessionId, assistantMessage.Id));

            return new JsonResult(new GenerateChatResponse
            {
                UserMessageId = userMessage.Id,
                UserMessageClientId = req.UserMessageClientId,
                UserMessageContent = userMessage.Content,
                UserMessageSentAt = userMessage.SentAt,
                UserMessageStatus = userMessage.Status,

                AssistantMessageId = assistantMessage.Id,
                AssistantMessageClientId = req.AssistantMessageClientId,
                AssistantMessageSentAt = assistantMessage.SentAt,
                AssistantMessageStatus = assistantMessage.Status,
            });
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }
    }
}
