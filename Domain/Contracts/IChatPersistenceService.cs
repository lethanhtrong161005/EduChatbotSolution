using Domain.DTOs;
using Domain.Entities;

namespace Domain.Contracts;

public interface IChatPersistenceService
{
    Task<ChatSession?> GetSessionByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ChatSession?> GetSessionWithMessagesByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ChatSessionHeader>> GetSessionHeadersByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ChatSession> CreateSessionAsync(
        Guid userId,
        int? subjectId,
        string? title,
        CancellationToken cancellationToken = default);

    Task<ChatSession> UpdateSessionTitleAsync(
        Guid sessionId,
        string title,
        CancellationToken cancellationToken = default);

    Task DeleteSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);



    Task<CreatedChatMessage> CreateUserMessageAsync(
        Guid sessionId,
        string content,
        CancellationToken cancellationToken = default);

    Task<CreatedChatMessage> CreateAssistantMessageAsync(
        Guid sessionId,
        string content,
        IReadOnlyList<ChunkRetrieval> chunkRetrievals,
        IReadOnlyList<ChunkRetrieval> chunkRetrievalsInContext,
        IReadOnlyList<ChunkUsage> chunkUsages,
        ChatGenerationSettings generationSettings,
        ChatGenerationMetrics generationMetrics,
        CancellationToken cancellationToken = default);
}
