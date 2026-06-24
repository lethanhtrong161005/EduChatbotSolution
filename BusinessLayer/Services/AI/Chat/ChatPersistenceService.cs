using AutoMapper;
using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;

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

        session?.Messages = [.. await GetMessagesBySessionAsync(id, limit, cxlTkn)];

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
            ],
            filter: e => e.ChatSessionId == sessionId,
            orderBy: q => q.OrderBy(e => e.SentAt),
            paginationSettings: limit.HasValue ? (limit.Value, 1) : (0, 0),
            asNoTracking: true,
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
        if (title == string.Empty)
            throw new EntityConstraintException("Title must not be empty");

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



    public async Task<ResolvedChatMessage> CreateUserMessageAsync(
        Guid sessionId,
        string content,
        CancellationToken cxlTkn = default)
    {
        var newMessage = _unitOfWork.ChatMessages.Insert(
            new ChatMessage
            {
                ChatSessionId = sessionId,
                ChatRole = ChatRole.User,
                Content = content,
                RawContent = content,
                SentAt = DateTime.UtcNow,
                Status = MessageStatus.Completed,
            });

        await _unitOfWork.SaveAsync(cxlTkn);

        var dto = _mapper.Map<ResolvedChatMessage>(newMessage);

        return dto;
    }

    public async Task<ResolvedChatMessage> CreateStreamingAssistantMessageAsync(
        Guid sessionId,
        CancellationToken cxlTkn = default)
    {
        var newMessage = _unitOfWork.ChatMessages.Insert(
            new ChatMessage
            {
                ChatSessionId = sessionId,
                ChatRole = ChatRole.Assistant,
                SentAt = DateTime.UtcNow,
                Status = MessageStatus.Pending,
            });

        await _unitOfWork.SaveAsync(cxlTkn);

        var dto = _mapper.Map<ResolvedChatMessage>(newMessage);

        return dto;
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
            throw new InvalidOperationException($"Cannot transition from {oldStatus} to {status}.");
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
        IReadOnlyList<ChunkRetrieval> chunkRetrievalsInContext,
        IReadOnlyList<ChunkUsage> chunkUsages,
        ChatGenerationSettings generationSettings,
        ChatGenerationMetrics generationMetrics,
        CancellationToken cxlTkn = default)
    {
        var message = (await _unitOfWork.ChatMessages.GetAsync(
            filter: e => e.Id == messageId,
            cancellationToken: cxlTkn))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("No assistant message matched the provided ID.");

        var resolvedCitations = await ResolveCitationsAsync(chunkUsages, cxlTkn);

        message.Content = content;
        message.RawContent = rawContent;
        message.Status = MessageStatus.Completed;

        message.GenerationSettings = new ChatMessageGenerationSettings
        {
            EmbeddingModel = generationSettings.EmbeddingModel,

            TopK = generationSettings.TopK,
            SimilarityThreshold = generationSettings.SimilarityThreshold,

            LlmModel = generationSettings.LlmModel,
            Temperature = generationSettings.Temperature,

            SystemPrompt = generationSettings.SystemPrompt,
            ContextPrompt = generationSettings.ContextPrompt,
            NoContextRetrievedPrompt = generationSettings.NoContextRetrievedPrompt,

            CitationExtractionTemperature = generationSettings.CitationExtractionTemperature,
            CitationExtractionPrompt = generationSettings.CitationExtractionPrompt,

            MaxContextChunks = generationSettings.MaxContextChunks,
            MaxHistoryMessages = generationSettings.MaxHistoryMessages,
        };

        message.GenerationMetrics = new ChatMessageGenerationMetrics
        {
            RetrievedChunkCount = chunkRetrievals.Count,
            ContextChunkCount = chunkRetrievalsInContext.Count,

            PromptTokens = generationMetrics.PromptTokens,
            CompletionTokens = generationMetrics.CompletionTokens,

            RetrievalTimeMs = generationMetrics.RetrievalTimeMs,
            TimeToFirstTokenMs = generationMetrics.TimeToFirstTokenMs,
            TotalResponseTimeMs = generationMetrics.TotalResponseTimeMs,
            TokensPerSecond = generationMetrics.TokensPerSecond,
        };

        message.Citations = [.. resolvedCitations.Select(
                    c => new Citation
                    {
                        ChunkId = c.ChunkId,
                        CitationIndex = c.CitationIndex,
                        SimilarityScore = c.SimilarityScore,
                        LocationInDocument = c.LocationInDocument,
                    })];

        await _unitOfWork.SaveAsync(cxlTkn);

        var dto = new ResolvedChatMessage
        {
            Id = message.Id,
            ChatRole = message.ChatRole,
            Content = message.Content,
            SentAt = message.SentAt,
            Status = message.Status,
            GenerationErrors = message.GenerationErrors,
            Citations = resolvedCitations,
        };

        return dto;
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
                    LocationInDocument = BuildLocation(),
                    ChunkIndex = chunk.ChunkIndex,
                    ChunkText = chunk.ChunkText,
                    DocumentTitle = chunk.Document.Title,
                    DocumentId = chunk.DocumentId,
                };

                string? BuildLocation()
                {
                    var locations = new List<string>();

                    if (chunk.PageNumber != null)
                    {
                        locations.Add($"Page: {chunk.PageNumber}");
                    }
                    if (!string.IsNullOrWhiteSpace(chunk.SectionTitle))
                    {
                        locations.Add($"Section: {chunk.SectionTitle}");
                    }

                    return locations.Count > 0
                        ? string.Join(" • ", locations)
                        : null;
                }
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
