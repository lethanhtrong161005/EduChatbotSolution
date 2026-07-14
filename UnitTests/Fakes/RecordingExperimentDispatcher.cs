using Domain.Contracts;

namespace UnitTests.Fakes;

public sealed class RecordingExperimentDispatcher : IExperimentDispatcher
{
    public List<Guid> ExperimentIds { get; } = [];
    public string Enqueue(Guid experimentId) { ExperimentIds.Add(experimentId); return experimentId.ToString(); }
}
