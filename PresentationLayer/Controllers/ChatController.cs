using AutoMapper;
using Domain.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Extensions;
using Presentation.Models;

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
        var userId = User.GetUserId();

        var subjects = await _subjectService.GetAccessibleSubjectsAsync(userId, cxlTkn);
        var sessions = await _chatPersistenceService.GetSessionHeadersByUserAsync(userId, cxlTkn);

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
            Sessions = _mapper.Map<List<ChatSidebarSessionVm>>(sessions),
        };

        return View(vm);
    }

    [HttpGet("session/{id:guid}")]
    public async Task<IActionResult> GetSession(Guid id)
    {
        var session = await _chatPersistenceService.GetSessionWithMessagesByIdAsync(id);

        if (session == null || session.UserId != User.GetUserId())
            return NotFound();

        var dto = _mapper.Map<ChatSessionDto>(session);

        return Ok(dto);
    }
}
