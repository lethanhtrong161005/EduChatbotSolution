using AutoMapper;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Presentation.DTOs;

namespace Presentation.RealtimeWeb;

public class ChatGenerationCoordinator(
    ISubjectService subjectService,
    IAiConfigurationResolver aiConfigResolver,
    IChatPersistenceService chatPersistenceService,
    IChatGenerationService chatGenerationService,
    IHubContext<AiChatHub, IAiChatClient> chatHub,
    IMapper mapper)
    : IChatGenerationCoordinator
{
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IAiConfigurationResolver _aiConfigResolver = aiConfigResolver;
    private readonly IChatPersistenceService _chatPersistenceService = chatPersistenceService;
    private readonly IChatGenerationService _chatGenerationService = chatGenerationService;
    private readonly IHubContext<AiChatHub, IAiChatClient> _chatHub = chatHub;
    private readonly IMapper _mapper = mapper;

    public async Task GenerateAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cxlTkn = default)
    {
        try
        {
            var session = await _chatPersistenceService.GetSessionWithMessagesByIdAsync(sessionId, cxlTkn);
            if (session == null)
                return;

            var targetAssistantMessage = session.Messages.FirstOrDefault(e => e.Id == assistantMessageId);
            if (targetAssistantMessage == null)
                return;

            var allowedSubjectIds = session.SubjectId.HasValue
                ? [session.SubjectId.Value]
                : (await _subjectService.GetAccessibleSubjectsAsync(session.UserId, cxlTkn))
                    .Select(e => e.Id);

            var aiConfig = await _aiConfigResolver.GetAiConfigurationAsync(session.SubjectId, cxlTkn);

            var chatHistory = session.Messages
                .Where(e => e.Status == MessageStatus.Completed
                            && e.SentAt < targetAssistantMessage.SentAt)
                .OrderBy(e => e.SentAt)
                .TakeLast(aiConfig.MaxHistoryMessages)
                .Select(e => new ChatHistoryMessage
                {
                    ChatRole = e.ChatRole,
                    Content = e.Content,
                })
                .ToList();

            var latestUserMessage = chatHistory.Last(e => e.ChatRole == ChatRole.User);

            var request = new ChatGenerationRequest
            {
                UserMessage = latestUserMessage.Content,
                AllowedSubjects = [.. allowedSubjectIds],
                ChatHistory = chatHistory,
                Settings = new ChatGenerationSettings
                {
                    EmbeddingModel = aiConfig.EmbeddingModel,

                    TopK = aiConfig.TopK,
                    SimilarityThreshold = aiConfig.SimilarityThreshold,

                    LlmModel = aiConfig.LlmModel,
                    Temperature = aiConfig.Temperature,

                    SystemPrompt = aiConfig.SystemPrompt,
                    ContextPrompt = aiConfig.ContextPrompt,
                    NoContextRetrievedPrompt = aiConfig.NoContextRetrievedPrompt,

                    CitationExtractionTemperature = aiConfig.CitationExtractionTemperature,
                    CitationExtractionPrompt = aiConfig.CitationExtractionPrompt,

                    MaxContextChunks = aiConfig.MaxContextChunks,
                    MaxHistoryMessages = aiConfig.MaxHistoryMessages,
                }
            };

            var result = await _chatGenerationService.GenerateAsync(
                    request,
                    async token =>
                    {
                        await _chatHub.Clients
                            .Group(HubGroups.Chat(sessionId))
                            .ReceiveToken(assistantMessageId, token);
                    },
                    cxlTkn);

            var resolvedChatMessage = await _chatPersistenceService.CompleteAssistantMessageAsync(
                   assistantMessageId,
                   result.Answer,
                   result.ChunkRetrievals,
                   result.ChunkRetrievalsInContext,
                   result.ChunkUsages,
                   request.Settings,
                   result.Metrics,
                   cxlTkn);

            var dto = _mapper.Map<ChatMessageDto>(resolvedChatMessage);

            await _chatHub.Clients
                .Group(HubGroups.Chat(sessionId))
                .GenerationCompleted(assistantMessageId, dto);
        }
        catch (Exception ex)
        {
            await _chatHub.Clients
                .Group(HubGroups.Chat(sessionId))
                .GenerationFailed(assistantMessageId, ex.Message);

            await _chatPersistenceService.FailAssistantMessageAsync(
                assistantMessageId,
                ex.Message,
                cxlTkn);

            throw;
        }
    }
}
