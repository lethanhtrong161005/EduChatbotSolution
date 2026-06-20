using AutoMapper;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Presentation.DTOs;

namespace Presentation.RealtimeWeb;

public class ChatGenerationCoordinator(
    IChatPersistenceService chatPersistenceService,
    IChatGenerationService chatGenerationService,
    IHubContext<AiChatHub, IAiChatClient> chatHub,
    IMapper mapper)
    : IChatGenerationCoordinator
{
    private readonly IChatPersistenceService _chatPersistenceService = chatPersistenceService;
    private readonly IChatGenerationService _chatGenerationService = chatGenerationService;
    private readonly IHubContext<AiChatHub, IAiChatClient> _chatHub = chatHub;
    private readonly IMapper _mapper = mapper;

    public async Task GenerateAsync(
        Guid sessionId,
        Guid assistantMessageId,
        Guid assistantMessageClientId,
        CancellationToken cxlTkn = default)
    {
        try
        {
            var session = await _chatPersistenceService.GetSessionWithMessagesByIdAsync(sessionId, cxlTkn);

            if (session == null)
                return;

            var history = session.Messages
                .OrderBy(e => e.CreatedAt)
                .TakeLast(20)
                .Select(e => new ChatHistoryMessage
                {
                    ChatRole = e.ChatRole,
                    Content = e.Content
                })
                .ToList();

            var latestUserMessage = session.Messages
                    .OrderByDescending(e => e.CreatedAt)
                    .First(e => e.ChatRole == ChatRole.User);

            var request = new ChatGenerationRequest
            {
                UserMessage = latestUserMessage.Content,
                AllowedSubjects = [],
                ChatHistory = history,
                Settings = new ChatGenerationSettings
                {
                    TopK = 5,
                    SimilarityThreshold = 0.6,
                    LlmModel = "dummy",
                    Temperature = 0.7,
                    SystemPrompt = "",
                    MaxContextChunks = 5,
                    MaxHistoryMessages = 20,
                }
            };

            var result = await _chatGenerationService.GenerateAsync(
                    request,
                    async token =>
                    {
                        await _chatHub.Clients
                            .Group(HubGroups.Chat(sessionId))
                            .ReceiveToken(assistantMessageClientId, token);
                    },
                    cxlTkn);

            var createdChatMessage = await _chatPersistenceService.CompleteAssistantMessageAsync(
                   assistantMessageId,
                   result.Answer,
                   result.ChunkRetrievals,
                   result.ChunkRetrievalsInContext,
                   result.ChunkUsages,
                   request.Settings,
                   result.Metrics,
                   cxlTkn);

            var dto = _mapper.Map<ChatMessageDto>(createdChatMessage);

            await _chatHub.Clients
                .Group(HubGroups.Chat(sessionId))
                .GenerationCompleted(assistantMessageClientId, dto);
        }
        catch (Exception ex)
        {
            await _chatHub.Clients
                .Group(HubGroups.Chat(sessionId))
                .GenerationFailed(assistantMessageClientId, ex.ToString());
        }
    }
}
