using Domain.Common;
using Domain.Contracts;
using Hangfire;

namespace Presentation.Background;

public sealed class ExperimentJob(IExperimentRunner runner)
{
    [Retry(Retries = 0)]
    [Queue(HangfireConstants.LowPriorityQueue)]
    public Task RunAsync(Guid experimentId, CancellationToken cxlTkn = default) => runner.RunAsync(experimentId, cxlTkn);
}
