using AutoMapper;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.AspNetCore.SignalR;
using Presentation.DTOs;

namespace Presentation.Realtime;

public class ChatGenerationCoordinator(
    ISubjectService subjectService,
    IAiConfigurationResolver aiConfigResolver,
    IChatPersistenceService chatPersistenceService,
    IChatGenerationService chatGenerationService,
    IHubContext<AiChatHub, IAiChatClient> chatHub,
    IResourceRealtimeNotifier notifier,
    IMapper mapper)
    : IChatGenerationCoordinator
{
    private readonly ISubjectService _subjectService = subjectService;
    private readonly IAiConfigurationResolver _aiConfigResolver = aiConfigResolver;
    private readonly IChatPersistenceService _chatPersistenceService = chatPersistenceService;
    private readonly IChatGenerationService _chatGenerationService = chatGenerationService;
    private readonly IHubContext<AiChatHub, IAiChatClient> _chatHub = chatHub;
    private readonly IResourceRealtimeNotifier _notifier = notifier;
    private readonly IMapper _mapper = mapper;

    public async Task GenerateTitleAsync(
        Guid sessionId,
        CancellationToken cxlTkn = default)
    {
        var session = await _chatPersistenceService.GetSessionWithMessagesByIdAsync(sessionId, 2, cxlTkn)
                      ?? throw new EntityNotFoundException($"Could not find parent session. Provided ID: {sessionId}");

        var aiConfig = await _aiConfigResolver.GetAiConfigurationAsync(session.SubjectId, cxlTkn);

        var userMessage = session.Messages.First(e => e.ChatRole == ChatRole.User);
        var assistantMessage = session.Messages.First(e => e.ChatRole == ChatRole.Assistant);

        var request = new TitleGenerationRequest
        {
            UserMessage = userMessage.RawContent,
            AssistantMessage = assistantMessage.RawContent,
            Attachments = [],
            Settings = new TitleGenerationSettings
            {
                LlmModel = aiConfig.LlmModel,
                Temperature = aiConfig.TitleTemperature,
                SystemPrompt = aiConfig.TitlePrompt,
            },
        };

        var result = await _chatGenerationService.GenerateTitleAsync(request, cxlTkn);

        var updatedSession = await _chatPersistenceService.UpdateSessionTitleAsync(
                   session.Id,
                   result.Title,
                   request.Settings,
                   result.Metrics,
                   cxlTkn);

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.ChatSession,
            Action = ResourceAction.Updated,
            ResourceId = updatedSession.Id.ToString(),
            ResourceName = updatedSession.Title,
            Properties =
            {
                { nameof(ChatSession.UserId), updatedSession.UserId.ToString() },
                { nameof(ChatSession.SubjectId), updatedSession.SubjectId.ToString() },
            },
        };

        await _notifier.PushUpdateAsync(update);
    }

    public async Task GenerateChatAsync(
        Guid sessionId,
        Guid assistantMessageId,
        Guid assistantMessageClientId,
        CancellationToken cxlTkn = default)
    {
        try
        {
            var session = await _chatPersistenceService.GetSessionWithMessagesByIdAsync(sessionId, cancellationToken: cxlTkn)
                          ?? throw new EntityNotFoundException($"Could not find parent session. Provided ID: {sessionId}");

            var targetAssistantMessage = session.Messages.FirstOrDefault(e => e.Id == assistantMessageId)
                                         ?? throw new EntityNotFoundException($"Could not find target asssistant message. Provided ID: {assistantMessageId}");

            if (targetAssistantMessage.Status != MessageStatus.Pending)
                throw new InvalidOperationException("Assistant message is not pending content generation.");

            var allowedSubjectIds = session.SubjectId.HasValue
                ? [session.SubjectId.Value]
                : (await _subjectService.GetAccessibleSubjectsAsync(session.UserId, cancellationToken: cxlTkn))
                    .Select(e => e.Id);

            var request = await BuildChatGenerationRequest(session, targetAssistantMessage, allowedSubjectIds, cxlTkn);

            if (await _chatPersistenceService.UpdateAssistantMessageStatusAsync(
                targetAssistantMessage.Id, MessageStatus.Generating, cxlTkn))
            {
                await _chatHub.Clients
                           .Group(HubGroups.Chat(sessionId))
                           .GenerationStarted(assistantMessageId, assistantMessageClientId);
            }

            var result = await _chatGenerationService.GenerateChatAsync(
                    request,
                    async token =>
                    {
                        await _chatHub.Clients
                            .Group(HubGroups.Chat(sessionId))
                            .ReceiveToken(assistantMessageId, assistantMessageClientId, token);
                    },
                    cxlTkn);

            var resolvedChatMessage = await _chatPersistenceService.CompleteAssistantMessageAsync(
                   assistantMessageId,
                   result.Answer,
                   result.RawAnswer,
                   result.ChunkRetrievals,
                   result.ChunkRetrievalsInContext,
                   result.ChunkUsages,
                   request.Settings,
                   result.Metrics,
                   cxlTkn);

            var dto = _mapper.Map<ChatMessageDto>(resolvedChatMessage);

            await _chatHub.Clients
                .Group(HubGroups.Chat(sessionId))
                .GenerationCompleted(assistantMessageId, assistantMessageClientId, dto);
        }
        catch (Exception ex)
        {
            await _chatPersistenceService.FailAssistantMessageAsync(assistantMessageId, ex.Message, cxlTkn);

            await _chatHub.Clients
                .Group(HubGroups.Chat(sessionId))
                .GenerationFailed(assistantMessageId, assistantMessageClientId, ex.Message);

            throw;
        }
    }

    async Task<ChatGenerationRequest> BuildChatGenerationRequest(
        ChatSession session,
        ChatMessage targetAssistantMessage,
        IEnumerable<int> allowedSubjectIds,
        CancellationToken cxlTkn)
    {
        var aiConfig = await _aiConfigResolver.GetAiConfigurationAsync(session.SubjectId, cxlTkn);

        var chatHistory = session.Messages
            .Where(e => e.Status == MessageStatus.Completed
                        && e.SentAt < targetAssistantMessage.SentAt)
            .OrderBy(e => e.SentAt)
            .TakeLast(aiConfig.MaxHistoryMessages)
            .Select(e => new ChatHistoryMessage
            {
                ChatRole = e.ChatRole,
                Content = e.RawContent,
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
                Temperature = aiConfig.ChatTemperature,

                SystemPrompt = aiConfig.ChatPrompt,
                ContextPrompt = aiConfig.ContextPrompt,
                NoContextRetrievedPrompt = aiConfig.NoContextRetrievedPrompt,

                CitationExtractionTemperature = aiConfig.CitationExtractionTemperature,
                CitationExtractionPrompt = aiConfig.CitationExtractionPrompt,

                MaxContextChunks = aiConfig.MaxContextChunks,
                MaxHistoryMessages = aiConfig.MaxHistoryMessages,
            }
        };

        return request;
    }
}
