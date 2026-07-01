using AutoMapper;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Presentation.Background;
using Presentation.DTOs;
using Presentation.Extensions;
using Presentation.Realtime;
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
    IHubContext<AiChatHub, IAiChatClient> chatHub,
    IResourceRealtimeNotifier notifier,
    IMapper mapper)
    : PageModel
{
    private readonly IChatPersistenceService _chatPersistenceService = chatPersistenceService;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IHubContext<AiChatHub, IAiChatClient> _chatHub = chatHub;
    private readonly IResourceRealtimeNotifier _notifier = notifier;
    private readonly IMapper _mapper = mapper;

    /// <summary>
    /// Gets the chat page view model rendered by the page.
    /// </summary>
    public ChatPageVm ViewModel { get; private set; } = new();

    [FromHeader]
    public string CallerConnectionId { get; set; } = string.Empty;

    [FromHeader]
    public string ChatConnectionId { get; set; } = string.Empty;

    /// <summary>
    /// Loads the chat page for the current user and optional session.
    /// </summary>
    /// <param name="id">Optional active chat session identifier.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    /// <returns>The chat page, unauthorized, or not found result.</returns>
    public async Task<IActionResult> OnGetAsync(Guid? id, CancellationToken cxlTkn)
    {
        var userId = User.GetUserId();

        if (id.HasValue)
        {
            var session = await _chatPersistenceService.GetSessionInfoByIdAsync(id.Value, cxlTkn);

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
            var subjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cancellationToken: cxlTkn);
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
            var sessions = await _chatPersistenceService.GetSessionInfosByUserAsync(userId, cxlTkn);
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
        var session = await _chatPersistenceService.GetSessionWithMessagesByIdAsync(id, cancellationToken: cxlTkn);

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
    public async Task<IActionResult> OnPostCreateSessionAsync(
        [FromForm] CreateChatSessionRequest req,
        CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();

            if (!req.SubjectId.HasValue)
            {
                var subjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cancellationToken: cxlTkn);
                if (!subjects.Any())
                    return BadRequest();
            }

            if (req.SubjectId.HasValue
                && !await _subjectService.IsMemberAsync(req.SubjectId.Value, userId, cxlTkn))
            {
                return BadRequest();
            }

            var session = await _chatPersistenceService.CreateSessionAsync(
                userId,
                req.SubjectId,
                !string.IsNullOrWhiteSpace(req.MessageContent)
                    ? GetMessageSnippet(req.MessageContent)
                    : $"Session {DateTime.UtcNow:f}",
                cxlTkn);

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.ChatSession,
                Action = ResourceAction.Created,
                ResourceId = session.Id.ToString(),
                ResourceName = session.Title,
                Properties =
                {
                    { nameof(ChatSession.UserId), session.UserId.ToString() },
                    { nameof(ChatSession.SubjectId), session.SubjectId.ToString() },
                },
            };

            await _notifier.PushUpdateAsync(update, CallerConnectionId);

            var res = _mapper.Map<CreateChatSessionResponse>(session);
            return new JsonResult(res);
        }
        catch (UserClaimException)
        {
            return Unauthorized();
        }

        static string GetMessageSnippet(string content)
        {
            content = content[..Math.Min(40, content.Length)];
            return content[..content.LastIndexOf(' ')];
        }
    }

    /// <summary>
    /// Generates the next AI message in a chat session.
    /// </summary>
    /// <param name="req">Message generation request.</param>
    /// <param name="cxlTkn">A token used to cancel the request.</param>
    public async Task<IActionResult> OnPostGenerateAsync(
        [FromForm] GenerateChatRequest req,
        CancellationToken cxlTkn)
    {
        try
        {
            var userId = User.GetUserId();
            var session = await _chatPersistenceService.GetSessionInfoByIdAsync(req.SessionId, cxlTkn);

            if (session == null || session.UserId != userId)
                return NotFound();

            var userMessage = await _chatPersistenceService.CreateUserMessageAsync(session.Id, req.Content, cxlTkn);
            var assistantMessage = await _chatPersistenceService.CreateStreamingAssistantMessageAsync(session.Id, cxlTkn);

            await _chatHub.Clients
                .GroupExcept(HubGroups.Chat(session.Id), ChatConnectionId)
                .ExchangeCreated(userMessage.Id, req.UserMessageClientId, assistantMessage.Id, req.AssistantMessageClientId);

            var chatJob = BackgroundJob.Enqueue<IChatGenerationCoordinator>(
                HangfireConstants.HighPriorityQueue,
                e => e.GenerateChatAsync(session.Id, assistantMessage.Id, req.AssistantMessageClientId));

            if (session.MessageCount == 0)
            {
                BackgroundJob.ContinueJobWith<IChatGenerationCoordinator>(
                    chatJob,
                    HangfireConstants.MediumPriorityQueue,
                    e => e.GenerateTitleAsync(session.Id));
            }

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
