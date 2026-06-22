using Domain.Common;

namespace Domain.Contracts;

public interface IChatGenerationCoordinator
{
    [Retry(Retries = 0)]
    Task GenerateAsync(
        Guid sessionId,
        Guid assistantMessageId,
        CancellationToken cancellationToken = default);
}
