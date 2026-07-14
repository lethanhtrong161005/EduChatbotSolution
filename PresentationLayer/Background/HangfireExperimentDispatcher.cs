using Domain.Contracts;
using Hangfire;

namespace Presentation.Background;

public sealed class HangfireExperimentDispatcher : IExperimentDispatcher
{
    public string Enqueue(Guid experimentId) => BackgroundJob.Enqueue<ExperimentJob>(job => job.RunAsync(experimentId, CancellationToken.None));
}
