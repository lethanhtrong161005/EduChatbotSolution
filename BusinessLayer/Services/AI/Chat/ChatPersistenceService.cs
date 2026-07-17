using AutoMapper;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Utils;

namespace Business.Services.AI.Chat;

public class ChatPersistenceService(
    IUnitOfWork unitOfWork,
    IMapper mapper)
    : IChatPersistenceService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;

    public async Task<ChatSessionInfo?> GetSessionInfoByIdAsync(
        Guid id,
        CancellationToken cxlTkn = default)
    {
        return (await _unitOfWork.ChatSessions.GetAsync(
             preFilter: e => e.Id == id,
             projection: e => new ChatSessionInfo
             {
                 Id = e.Id,
                 UserId = e.UserId,
                 SubjectId = e.SubjectId,
                 Title = e.Title,
                 LastMessageAt = e.Messages.Max(e => (DateTime?)e.SentAt) ?? e.CreatedAt,
                 MessageCount = e.Messages.Count,
             },
             asNoTracking: true,
             cancellationToken: cxlTkn))
             .FirstOrDefault();
    }

    public async Task<IEnumerable<ChatSessionInfo>> GetSessionInfosByUserAsync(
        Guid userId,
        CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.ChatSessions.GetAsync(
            preFilter: e => e.UserId == userId,
            projection: e => new ChatSessionInfo
            {
                Id = e.Id,
                UserId = e.UserId,
                SubjectId = e.SubjectId,
                Title = e.Title,
                LastMessageAt = e.Messages.Max(e => (DateTime?)e.SentAt) ?? e.CreatedAt,
                MessageCount = e.Messages.Count,
            },
            orderBy: q => q.OrderByDescending(x => x.LastMessageAt),
            asNoTracking: true,
            cancellationToken: cxlTkn);
    }

    public async Task<ChatSession?> GetSessionWithMessagesByIdAsync(
        Guid id,
        int? limit = null,
        CancellationToken cxlTkn = default)
    {
        var session = (await _unitOfWork.ChatSessions.GetAsync(
            filter: e => e.Id == id,
            asNoTracking: true,
            cancellationToken: cxlTkn))
            .FirstOrDefault();

        if (session == null)
            return null;

        List<ChatMessage> messages = [.. await GetMessagesBySessionAsync(id, limit, cxlTkn)];

        foreach (var message in messages)
        {
            session.Messages.Add(message);
        }

        return session;
    }

    private async Task<IEnumerable<ChatMessage>> GetMessagesBySessionAsync(
        Guid sessionId,
        int? limit = null,
        CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.ChatMessages.GetAsync(
            includeProperties:
            [
                nameof(ChatMessage.Citations),
                nameof(ChatMessage.Citations) + "." + nameof(Citation.Chunk),
                nameof(ChatMessage.Citations) + "." + nameof(Citation.Chunk) + "." + nameof(Chunk.Document),
                nameof(ChatMessage.Citations) + "." + nameof(Citation.RetrievalSnapshot),
                nameof(ChatMessage.InReplyToMessage),
                nameof(ChatMessage.InReplyToMessage) + "." + nameof(ChatMessage.AssistantVariants),
            ],
            filter: e => e.ChatSessionId == sessionId && (e.ChatRole != ChatRole.Assistant || e.IsSelectedVariant),
            orderBy: q => q.OrderBy(e => e.MessageIndex),
            paginationSettings: limit.HasValue ? (limit.Value, 1) : (0, 0),
            //asNoTracking: true,
            cancellationToken: cxlTkn);
    }

    public async Task<ChatSession> CreateSessionAsync(
        Guid userId,
        int? subjectId,
        string title,
        CancellationToken cancellationToken = default)
    {
        var newSession = _unitOfWork.ChatSessions.Insert(
            new ChatSession
            {
                UserId = userId,
                SubjectId = subjectId,
                Title = title,
            });

        await _unitOfWork.SaveAsync(cancellationToken);
        return newSession;
    }

    public async Task<ChatSession> UpdateSessionTitleAsync(
        Guid sessionId,
        string title,
        TitleGenerationSettings settings,
        TitleGenerationMetrics metrics,
        CancellationToken cxlTkn = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new EntityValidationException("Title must not be empty", nameof(ChatSession.Title));

        var session = await _unitOfWork.ChatSessions.FindByIdAsync(sessionId, cxlTkn)
                      ?? throw new EntityNotFoundException("No chat session matched the provided ID.");

        session.Title = title;

        session.TitleGenerationSettings = new ChatSessionTitleGenerationSettings
        {
            LlmModel = settings.LlmModel,
            Temperature = settings.Temperature,
            SystemPrompt = settings.SystemPrompt,
        };

        session.TitleGenerationMetrics = new ChatSessionTitleGenerationMetrics
        {
            PromptTokens = metrics.PromptTokens,
            CompletionTokens = metrics.CompletionTokens,
            ResponseTimeMs = metrics.ResponseTimeMs,
        };

        await _unitOfWork.SaveAsync(cxlTkn);

        return session;
    }

    public async Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken cxlTkn = default)
    {
        var session = await _unitOfWork.ChatSessions.FindByIdAsync(sessionId, cxlTkn)
                      ?? throw new EntityNotFoundException("No chat session matched the provided ID.");

        _unitOfWork.ChatSessions.Delete(session);
        await _unitOfWork.SaveAsync(cxlTkn);
    }



    public async Task<ChatExchangeResult> CreateExchangeAsync(
        Guid sessionId,
        string userContent,
        CancellationToken cxlTkn = default)
    {
        var (userEntity, assistantEntity) = await _unitOfWork.ChatTurns.CreateExchangeAsync(sessionId, userContent, cxlTkn);
        var userMessage = _mapper.Map<ResolvedChatMessage>(userEntity);
        var assistantMessage = await ResolveAssistantMessageAsync(assistantEntity, cancellationToken: cxlTkn);

        return new ChatExchangeResult
        {
            UserMessage = userMessage,
            AssistantMessage = assistantMessage,
        };
    }

    public async Task<ResolvedChatMessage> ResetFailedAssistantMessageAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cxlTkn = default)
    {
        var message = await _unitOfWork.ChatTurns.ResetFailedAssistantMessageAsync(sessionId, assistantMessageId, cxlTkn);
        return await ResolveAssistantMessageAsync(message, cancellationToken: cxlTkn);
    }

    public async Task<ResolvedChatMessage> CreateAssistantVariantAsync(
        Guid sessionId,
        Guid completedAssistantMessageId,
        CancellationToken cxlTkn = default)
    {
        var message = await _unitOfWork.ChatTurns.CreateAssistantVariantAsync(sessionId, completedAssistantMessageId, cxlTkn);
        return await ResolveAssistantMessageAsync(message, cancellationToken: cxlTkn);
    }

    public async Task<ResolvedChatMessage?> GetAssistantVariantAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cxlTkn = default)
    {
        var message = await _unitOfWork.ChatTurns.GetAssistantVariantAsync(sessionId, assistantMessageId, cxlTkn);
        return message is null ? null : await ResolveAssistantMessageAsync(message, cancellationToken: cxlTkn);
    }

    public async Task<ResolvedChatMessage> SelectAssistantVariantAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cxlTkn = default)
    {
        var message = await _unitOfWork.ChatTurns.SelectAssistantVariantAsync(sessionId, assistantMessageId, cxlTkn);
        return await ResolveAssistantMessageAsync(message, cancellationToken: cxlTkn);
    }

    public async Task<bool> UpdateAssistantMessageStatusAsync(
        Guid messageId,
        MessageStatus status,
        CancellationToken cxlTkn = default)
    {
        var message = (await _unitOfWork.ChatMessages.GetAsync(
            filter: e => e.Id == messageId,
            cancellationToken: cxlTkn))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("No assistant message matched the provided ID.");

        var oldStatus = message.Status;

        if (status == oldStatus)
            return false;

        if ((status == MessageStatus.Generating && oldStatus != MessageStatus.Pending)
            || (status == MessageStatus.Pending && oldStatus == MessageStatus.Generating))
        {
            throw new EntityConflictException($"Cannot transition from {oldStatus} to {status}.");
        }

        message.Status = status;

        await _unitOfWork.SaveAsync(cxlTkn);
        return true;
    }

    public async Task<ResolvedChatMessage> CompleteAssistantMessageAsync(
        Guid messageId,
        string content,
        string rawContent,
        IReadOnlyList<ChunkRetrieval> chunkRetrievals,
        IReadOnlyList<RetrievedContextSnapshot> retrievedContexts,
        IReadOnlyList<NormalizedRequestMessageSnapshot> requestMessages,
        IReadOnlyList<ResolvedSubjectSnapshot> resolvedSubjects,
        IReadOnlyList<ChunkUsage> chunkUsages,
        ChatGenerationSettings generationSettings,
        ChatGenerationMetrics generationMetrics,
        CancellationToken cxlTkn = default)
    {
        SequentialIndexValidator.EnsureExact(retrievedContexts, e => e.RetrievalRank, "Retrieved chat contexts");
        SequentialIndexValidator.EnsureExact(retrievedContexts.Where(e => e.WasIncludedInPrompt), e => e.PromptOrder ?? 0, "Prompt chat contexts");
        SequentialIndexValidator.EnsureExact(requestMessages, e => e.MessageOrder, "Normalized chat request messages");
        SequentialIndexValidator.EnsureExact(resolvedSubjects, e => e.SubjectOrder, "Resolved chat subjects");
        if (retrievedContexts.Any(e => e.WasIncludedInPrompt != e.PromptOrder.HasValue)) throw new InvalidOperationException("Retrieved chat prompt membership and prompt order disagree.");
        var message = (await _unitOfWork.ChatMessages.GetAsync(
            filter: e => e.Id == messageId,
            cancellationToken: cxlTkn))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("No assistant message matched the provided ID.");

        var resolvedCitations = await ResolveCitationsAsync(chunkUsages, cxlTkn);

        message.Content = content;
        message.RawContent = rawContent;
        message.Status = MessageStatus.Completed;

        var persistedContexts = new Dictionary<Guid, ChatMessageContext>();
        foreach (var context in retrievedContexts.OrderBy(item => item.RetrievalRank))
        {
            var persisted = new ChatMessageContext
            {
                ChatMessageId = message.Id,
                RetrievalRank = context.RetrievalRank,
                PromptOrder = context.PromptOrder,
                WasIncludedInPrompt = context.WasIncludedInPrompt,
                ChunkId = context.ChunkId,
                SourceChunkId = context.ChunkId,
                SourceDocumentId = context.SourceDocumentId,
                SourceSubjectId = context.SourceSubjectId,
                ChunkIndex = context.ChunkIndex,
                ChunkText = context.ChunkText,
                SimilarityScore = context.SimilarityScore,
                DocumentTitle = context.DocumentTitle,
                DocumentFileName = context.DocumentFileName,
                SubjectCode = context.SubjectCode,
                SubjectName = context.SubjectName,
                StartPageNumber = context.StartPageNumber,
                EndPageNumber = context.EndPageNumber,
                StartSectionTitle = context.StartSectionTitle,
                EndSectionTitle = context.EndSectionTitle,
            };
            message.RetrievedContexts.Add(persisted);
            persistedContexts.Add(context.ChunkId, persisted);
        }

        foreach (var requestMessage in requestMessages.OrderBy(e => e.MessageOrder)) message.RequestMessages.Add(new ChatMessageRequestMessage { MessageOrder = requestMessage.MessageOrder, Role = requestMessage.Role, Content = requestMessage.Content });
        foreach (var subject in resolvedSubjects.OrderBy(e => e.SubjectOrder)) message.ResolvedSubjects.Add(new ChatMessageSubjectSnapshot { SubjectOrder = subject.SubjectOrder, SubjectId = subject.SubjectId, SourceSubjectId = subject.SubjectId, SubjectCode = subject.SubjectCode, SubjectName = subject.SubjectName });

        message.GenerationSettings = new ChatMessageGenerationSettings
        {
            EmbeddingProvider = generationSettings.EmbeddingProvider,
            EmbeddingModel = generationSettings.EmbeddingModel,

            TopK = generationSettings.TopK,
            SimilarityThreshold = generationSettings.SimilarityThreshold,

            LlmProvider = generationSettings.LlmProvider,
            LlmModel = generationSettings.LlmModel,
            Temperature = generationSettings.Temperature,

            SystemPrompt = generationSettings.SystemPrompt,
            ContextPrompt = generationSettings.ContextPrompt,
            NoContextRetrievedPrompt = generationSettings.NoContextRetrievedPrompt,

            CitationExtractionTemperature = generationSettings.CitationExtractionTemperature,
            CitationExtractionPrompt = generationSettings.CitationExtractionPrompt,

            MaxContextChunks = generationSettings.MaxContextChunks,
            MaxHistoryMessages = generationSettings.MaxHistoryMessages,
            ReasoningEffort = generationSettings.ReasoningEffort,
            ReasoningOutput = generationSettings.ReasoningOutput,
        };

        message.GenerationMetrics = new ChatMessageGenerationMetrics
        {
            RetrievedChunkCount = chunkRetrievals.Count,
            ContextChunkCount = retrievedContexts.Count(e => e.WasIncludedInPrompt),

            PromptTokens = generationMetrics.PromptTokens,
            CompletionTokens = generationMetrics.CompletionTokens,

            RetrievalTimeMs = generationMetrics.RetrievalTimeMs,
            TimeToFirstTokenMs = generationMetrics.TimeToFirstTokenMs,
            TotalResponseTimeMs = generationMetrics.TotalResponseTimeMs,
            TokensPerSecond = generationMetrics.TokensPerSecond,
        };

        List<Citation> citations = [.. resolvedCitations.Select(
            c => new Citation
            {
                ChunkId = c.ChunkId,
                RetrievalSnapshot = persistedContexts.GetValueOrDefault(c.ChunkId) ?? throw new InvalidOperationException($"Citation chunk {c.ChunkId} is absent from the persisted retrieval snapshot."),
                CitationIndex = c.CitationIndex,
                SimilarityScore = c.SimilarityScore,
                LocationInDocument = c.LocationInDocument,
            })];

        foreach (var citation in citations)
        {
            message.Citations.Add(citation);
        }

        message.ReconstructionCompleteness = ReconstructionCompleteness.Complete;

        await _unitOfWork.SaveAsync(cxlTkn);

        return await ResolveAssistantMessageAsync(message, resolvedCitations, cxlTkn);
    }

    private async Task<ResolvedChatMessage> ResolveAssistantMessageAsync(
        ChatMessage message,
        IReadOnlyList<ResolvedCitation>? citations = null,
        CancellationToken cancellationToken = default)
    {
        var dto = _mapper.Map<ResolvedChatMessage>(message) with
        {
            Citations = citations ?? _mapper.Map<IReadOnlyList<ResolvedCitation>>(message.Citations),
            VariantNavigation = await CreateVariantNavigationAsync(message, cancellationToken),
        };

        return dto;
    }

    private async Task<ResolvedChatVariantNavigation> CreateVariantNavigationAsync(
        ChatMessage message,
        CancellationToken cancellationToken)
    {
        if (message.InReplyToMessageId is null || message.VariantIndex is null)
            throw new InvalidOperationException("The assistant message has no variant identity.");

        var variants = (await _unitOfWork.ChatMessages.GetAsync(
            filter: item => item.ChatSessionId == message.ChatSessionId && item.InReplyToMessageId == message.InReplyToMessageId,
            orderBy: query => query.OrderBy(item => item.VariantIndex),
            asNoTracking: true,
            cancellationToken: cancellationToken))
            .ToList();

        return new ResolvedChatVariantNavigation
        {
            UserMessageId = message.InReplyToMessageId.Value,
            CurrentVariantIndex = message.VariantIndex.Value,
            TotalVariantCount = variants.Count,
            Variants = [.. variants.Select(item => new ResolvedChatVariantOption
            {
                MessageId = item.Id,
                VariantIndex = item.VariantIndex!.Value,
                IsSelected = item.IsSelectedVariant,
                Status = item.Status,
            })],
        };
    }

    private async Task<IReadOnlyList<ResolvedCitation>> ResolveCitationsAsync(
        IEnumerable<ChunkUsage> chunkUsages,
        CancellationToken cxlTkn = default)
    {
        var chunkIds = chunkUsages
            .Select(c => c.ChunkId)
            .Distinct()
            .ToList();

        var chunks = await _unitOfWork.Chunks.GetAsync(
            filter: e => chunkIds.Contains(e.Id),
            includeProperties: [nameof(Chunk.Document)],
            cancellationToken: cxlTkn);

        var chunkLookup = chunks.ToDictionary(c => c.Id, c => c);

        return [.. chunkUsages
            .OrderBy(c => c.CitationIndex)
            .Select(chunkUsage =>
            {
                if (!chunkLookup.TryGetValue(chunkUsage.ChunkId, out var chunk))
                {
                    throw new EntityNotFoundException($"Chunk {chunkUsage.ChunkId} not found.");
                }

                return new ResolvedCitation
                {
                    ChunkId = chunkUsage.ChunkId,
                    CitationIndex = chunkUsage.CitationIndex,
                    SimilarityScore = chunkUsage.SimilarityScore,
                    LocationInDocument = chunk.BuildLocation(),
                    ChunkIndex = chunk.ChunkIndex,
                    ChunkText = chunk.ChunkText,
                    DocumentTitle = chunk.Document.Title,
                    DocumentId = chunk.DocumentId,
                };
            })];
    }

    public async Task FailAssistantMessageAsync(
        Guid messageId,
        string generationErrors,
        CancellationToken cxlTkn = default)
    {
        var message = await _unitOfWork.ChatMessages.FindByIdAsync(messageId, cxlTkn)
                      ?? throw new EntityNotFoundException("No assistant message matched the provided ID.");

        message.Status = MessageStatus.Failed;
        message.GenerationErrors = generationErrors;

        await _unitOfWork.SaveAsync(cxlTkn);
    }
}
