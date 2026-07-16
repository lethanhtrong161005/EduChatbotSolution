using Domain.Common;
using Domain.Contracts;
using Hangfire;

namespace Presentation.Background;

public sealed class DocumentIndexingJob(IDocumentIndexingCoordinator coordinator)
{
    [Retry(Retries = 4)]
    [Queue(HangfireConstants.LowPriorityQueue)]
    public Task IndexAsync(Guid documentId, CancellationToken cxlTkn = default) =>
         coordinator.IndexAsync(documentId, cxlTkn);
}