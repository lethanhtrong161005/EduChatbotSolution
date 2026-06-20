namespace Domain.Contracts;

public interface IChatGenerationCoordinator
{
    Task GenerateAsync(
        Guid sessionId,
        Guid assistantMessageId,
        Guid assistantMessageClientId,
        CancellationToken cancellationToken = default);
}
