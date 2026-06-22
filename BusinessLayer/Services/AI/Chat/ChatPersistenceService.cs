using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;

namespace Business.Services.AI.Chat;

public class ChatPersistenceService(IUnitOfWork unitOfWork) : IChatPersistenceService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ChatSession?> GetSessionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ChatSessions.FindByIdAsync(id, cancellationToken);
    }

    public async Task<ChatSession?> GetSessionWithMessagesByIdAsync(
       Guid id,
       CancellationToken cancellationToken = default)
    {
        var session = (await _unitOfWork.ChatSessions.GetAsync(
            filter: e => e.Id == id,
            asNoTracking: true,
            cancellationToken: cancellationToken))
            .FirstOrDefault();

        session?.Messages = [.. await GetMessagesBySessionAsync(id, cancellationToken)];

        return session;
    }

    private async Task<IEnumerable<ChatMessage>> GetMessagesBySessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
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
            asNoTracking: true,
            cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<ChatSessionHeader>> GetSessionHeadersByUserAsync(
          Guid userId,
          CancellationToken cancellationToken = default)
    {
        var sessionHeaders = await _unitOfWork.ChatSessions.GetAsync(
            preFilter: e => e.UserId == userId,
            projection: e => new ChatSessionHeader
            {
                Id = e.Id,
                Title = e.Title ?? string.Empty,
                LastMessageAt = e.Messages.Max(e => (DateTime?)e.SentAt) ?? e.CreatedAt,
            },
            orderBy: q => q.OrderByDescending(x => x.LastMessageAt),
            asNoTracking: true,
            cancellationToken: cancellationToken);

        return sessionHeaders;
    }

    public async Task<ChatSession> CreateSessionAsync(
          Guid userId,
          int? subjectId,
          string? title,
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
           CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.ChatSessions.FindByIdAsync(sessionId, cancellationToken)
                      ?? throw new EntityNotFoundException("No chat session matched the provided ID.");

        session.Title = title;

        var updatedSession = _unitOfWork.ChatSessions.Update(session);
        await _unitOfWork.SaveAsync(cancellationToken);

        return updatedSession;
    }

    public async Task DeleteSessionAsync(
          Guid sessionId,
          CancellationToken cancellationToken = default)
    {
        var session = await _unitOfWork.ChatSessions.FindByIdAsync(sessionId, cancellationToken)
                      ?? throw new EntityNotFoundException("No chat session matched the provided ID.");

        _unitOfWork.ChatSessions.Delete(session);
        await _unitOfWork.SaveAsync(cancellationToken);
    }



    public async Task<CreatedChatMessage> CreateUserMessageAsync(
          Guid sessionId,
          string content,
          CancellationToken cancellationToken = default)
    {
        var newMessage = _unitOfWork.ChatMessages.Insert(
            new ChatMessage
            {
                ChatSessionId = sessionId,
                ChatRole = ChatRole.User,
                Content = content,
                SentAt = DateTime.UtcNow,
            });

        await _unitOfWork.SaveAsync(cancellationToken);

        var messageDto = new CreatedChatMessage
        {
            Id = newMessage.Id,
            ChatRole = newMessage.ChatRole,
            Content = newMessage.Content,
            SentAt = newMessage.SentAt,
        };

        return messageDto;
    }

    public async Task<CreatedChatMessage> CreateStreamingAssistantMessageAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var newMessage = _unitOfWork.ChatMessages.Insert(
            new ChatMessage
            {
                ChatSessionId = sessionId,
                ChatRole = ChatRole.Assistant,
                SentAt = DateTime.UtcNow,
                //Status = ChatMessageStatus.Generating,
            });

        await _unitOfWork.SaveAsync(cancellationToken);

        var messageDto = new CreatedChatMessage
        {
            Id = newMessage.Id,
            ChatRole = newMessage.ChatRole,
            Content = newMessage.Content,
            SentAt = newMessage.SentAt,
        };

        return messageDto;
    }

    public async Task<CreatedChatMessage> CompleteAssistantMessageAsync(
        Guid messageId,
        string content,
        IReadOnlyList<ChunkRetrieval> chunkRetrievals,
        IReadOnlyList<ChunkRetrieval> chunkRetrievalsInContext,
        IReadOnlyList<ChunkUsage> chunkUsages,
        ChatGenerationSettings generationSettings,
        ChatGenerationMetrics generationMetrics,
        CancellationToken cancellationToken = default)
    {
        var resolvedCitations = await ResolveCitationsAsync(chunkUsages, cancellationToken);

        var message = (await _unitOfWork.ChatMessages.GetAsync(
            filter: e => e.Id == messageId,
            cancellationToken: cancellationToken))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("No assistant message matched the provided ID.");

        message.Content = content;
        message.SentAt = DateTime.UtcNow;

        message.GenerationSettings = new ChatMessageGenerationSettings
        {
            TopK = generationSettings.TopK,

            LlmModel = generationSettings.LlmModel,
            Temperature = generationSettings.Temperature,

            SystemPrompt = generationSettings.SystemPrompt,
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
                        QuotedText = c.QuotedText,
                        SimilarityScore = c.SimilarityScore,
                        LocationInDocument = c.LocationInDocument,
                    })];

        await _unitOfWork.SaveAsync(cancellationToken);

        var dto = new CreatedChatMessage
        {
            Id = message.Id,
            ChatRole = message.ChatRole,
            Content = message.Content,
            SentAt = message.SentAt,
            Citations = resolvedCitations,
        };

        return dto;
    }

    private async Task<IReadOnlyList<ResolvedCitation>> ResolveCitationsAsync(
          IEnumerable<ChunkUsage> usedChunks,
          CancellationToken cancellationToken = default)
    {
        var chunkIds = usedChunks
            .Select(c => c.ChunkId)
            .Distinct()
            .ToList();

        var chunks = await _unitOfWork.Chunks.GetAsync(
            filter: e => chunkIds.Contains(e.Id),
            includeProperties: [nameof(Chunk.Document)],
            cancellationToken: cancellationToken);

        var chunkLookup = chunks.ToDictionary(c => c.Id, c => c);

        return [.. usedChunks
            .OrderBy(c => c.CitationIndex)
            .Select(usedChunk =>
            {
                if (!chunkLookup.TryGetValue(usedChunk.ChunkId, out var chunk))
                {
                    throw new EntityNotFoundException($"Chunk {usedChunk.ChunkId} not found.");
                }

                return new ResolvedCitation
                {
                    ChunkId = usedChunk.ChunkId,
                    CitationIndex = usedChunk.CitationIndex,
                    QuotedText = usedChunk.QuotedText,
                    SimilarityScore = usedChunk.SimilarityScore,
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
}
