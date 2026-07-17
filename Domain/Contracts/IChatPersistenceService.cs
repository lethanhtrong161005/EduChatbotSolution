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

    Task<ChatExchangeResult> CreateExchangeAsync(
        Guid sessionId,
        string userContent,
        CancellationToken cancellationToken = default);

    Task<ResolvedChatMessage> ResetFailedAssistantMessageAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cancellationToken = default);

    Task<ResolvedChatMessage> CreateAssistantVariantAsync(
        Guid sessionId,
        Guid completedAssistantMessageId,
        CancellationToken cancellationToken = default);

    Task<ResolvedChatMessage?> GetAssistantVariantAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cancellationToken = default);

    Task<ResolvedChatMessage> SelectAssistantVariantAsync(
        Guid sessionId,
        Guid assistantMessageId,
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
        IReadOnlyList<RetrievedContextSnapshot> retrievedContexts,
        IReadOnlyList<NormalizedRequestMessageSnapshot> requestMessages,
        IReadOnlyList<ResolvedSubjectSnapshot> resolvedSubjects,
        IReadOnlyList<ChunkUsage> chunkUsages,
        ChatGenerationSettings generationSettings,
        ChatGenerationMetrics generationMetrics,
        CancellationToken cancellationToken = default);

    Task FailAssistantMessageAsync(
        Guid messageId,
        string generationErrors,
        CancellationToken cancellationToken = default);
}
