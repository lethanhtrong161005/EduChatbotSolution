using Domain.Common;
using Domain.Contracts;
using Hangfire;

namespace Presentation.Background;

public sealed class SubjectReindexJob(ISubjectReindexCoordinator coordinator)
{
    [Retry(Retries = 0)]
    [Queue(HangfireConstants.LowPriorityQueue)]
    public Task RunAsync(int subjectId, Domain.Contracts.DTOs.EffectiveAiConfiguration configuration, CancellationToken cxlTkn = default) =>
        coordinator.RunAsync(subjectId, configuration, cxlTkn);
}
