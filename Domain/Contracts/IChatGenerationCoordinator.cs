namespace Domain.Contracts;

public interface IChatGenerationCoordinator
{
    Task GenerateTitleAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task GenerateAnswerAsync(
        Guid sessionId,
        Guid assistantMessageId,
        Guid assistantMessageClientId,
        CancellationToken cancellationToken = default);
}
