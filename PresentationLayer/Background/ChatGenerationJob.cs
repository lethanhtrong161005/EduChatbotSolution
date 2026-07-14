using Domain.Common;
using Domain.Contracts;
using Hangfire;

namespace Presentation.Background;

public sealed class ChatGenerationJob(IChatGenerationCoordinator coordinator)
{
    [Retry(Retries = 0)]
    [Queue(HangfireConstants.HighPriorityQueue)]
    public Task GenerateAnswerAsync(Guid sessionId, Guid assistantMessageId, Guid assistantMessageClientId, CancellationToken cancellationToken = default) =>
        coordinator.GenerateAnswerAsync(sessionId, assistantMessageId, assistantMessageClientId, cancellationToken);

    [Retry(Retries = 2)]
    [Queue(HangfireConstants.MediumPriorityQueue)]
    public Task GenerateTitleAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        coordinator.GenerateTitleAsync(sessionId, cancellationToken);
}
