using Domain.Common;

namespace Domain.Contracts;

public interface IChatGenerationCoordinator
{
    [Retry(Retries = 2)]
    Task GenerateTitleAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    [Retry(Retries = 0)]
    Task GenerateChatAsync(
        Guid sessionId,
        Guid assistantMessageId,
        Guid assistantMessageClientId,
        CancellationToken cancellationToken = default);
}
