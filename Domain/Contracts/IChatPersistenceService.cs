using Domain.Contracts.DTOs;
using Domain.Entities;

namespace Domain.Contracts;

public interface IChatPersistenceService
{
    Task<ChatSessionInfo?> GetSessionInfoByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ChatSessionInfo>> GetSessionInfosByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ChatSession?> GetSessionWithMessagesByIdAsync(
        Guid id,
        int? limit = null,
        CancellationToken cancellationToken = default);

    Task<ChatSession> CreateSessionAsync(
        Guid userId,
        int? subjectId,
        string title,
        CancellationToken cancellationToken = default);

    Task<ChatSession> UpdateSessionTitleAsync(
        Guid sessionId,
        string title,
        TitleGenerationSettings settings,
        TitleGenerationMetrics metrics,
        CancellationToken cancellationToken = default);

    Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<ResolvedChatMessage> CreateUserMessageAsync(
        Guid sessionId,
        string content,
        CancellationToken cancellationToken = default);

    Task<ResolvedChatMessage> CreateStreamingAssistantMessageAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateAssistantMessageStatusAsync(
        Guid messageId,
        MessageStatus status,
        CancellationToken cancellationToken = default);

    Task<ResolvedChatMessage> CompleteAssistantMessageAsync(
        Guid messageId,
        string content,
        string rawContent,
        IReadOnlyList<ChunkRetrieval> chunkRetrievals,
        IReadOnlyList<ChunkRetrieval> chunkRetrievalsInContext,
        IReadOnlyList<ChunkUsage> chunkUsages,
        ChatGenerationSettings generationSettings,
        ChatGenerationMetrics generationMetrics,
        CancellationToken cancellationToken = default);

    Task FailAssistantMessageAsync(
        Guid messageId,
        string generationErrors,
        CancellationToken cancellationToken = default);
}
