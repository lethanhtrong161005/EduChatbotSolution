using AutoMapper;
using Domain.Contracts;
using Domain.Exceptions;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Constants;
using Presentation.DTOs;
using Presentation.Extensions;
using Presentation.ViewModels;

namespace Presentation.Controllers;

[Authorize]
[Route("chat")]
public class ChatController(
    IChatPersistenceService chatPersistenceService,
    ISubjectService subjectService,
    IMapper mapper) : Controller
{
    private readonly IChatPersistenceService _chatPersistenceService = chatPersistenceService;
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IMapper _mapper = mapper;

    [HttpGet("")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Index(Guid? id, CancellationToken cxlTkn)
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

        var vm = new ChatPageVm
        {
            ActiveSessionId = id,
            Subjects = _mapper.Map<List<SubjectSelectionVm>>(subjects),
        };

        return View(vm);
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessionHeaders(CancellationToken cxlTkn)
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

        var sessions = await _chatPersistenceService.GetSessionHeadersByUserAsync(userId, cxlTkn);

        var res = _mapper.Map<List<SessionHeaderDto>>(sessions);

        return Ok(res);
    }

    [HttpGet("session/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cxlTkn)
    {
        var session = await _chatPersistenceService.GetSessionWithMessagesByIdAsync(id, cxlTkn);

        if (session == null || session.UserId != User.GetUserId())
            return NotFound();

        var dto = _mapper.Map<ChatSessionDto>(session);

        return Ok(dto);
    }

    [HttpPost("session")]
    public async Task<IActionResult> CreateSession(
        CreateChatSessionRequest req,
        CancellationToken cxlTkn)
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

        if (!req.SubjectId.HasValue)
        {
            var subjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cxlTkn);
            if (!subjects.Any())
            {
                return BadRequest();
            }
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
        return Ok(res);
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(
        GenerateChatRequest req,
        CancellationToken cxlTkn)
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

        var session = await _chatPersistenceService.GetSessionByIdAsync(req.SessionId, cxlTkn);

        if (session == null || session.UserId != userId)
            return NotFound();

        var userMessage = await _chatPersistenceService.CreateUserMessageAsync(req.SessionId, req.Content, cxlTkn);
        var assistantMessage = await _chatPersistenceService.CreateStreamingAssistantMessageAsync(req.SessionId, cxlTkn);

        BackgroundJob.Enqueue<IChatGenerationCoordinator>(
            HangfireConstants.HighPriorityQueue,
            e => e.GenerateAsync(
                req.SessionId,
                assistantMessage.Id));

        return Ok(new GenerateChatResponse
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
}
