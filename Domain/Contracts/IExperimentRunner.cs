namespace Domain.Contracts;

public interface IExperimentRunner
{
    Task RunAsync(Guid experimentId, CancellationToken cancellationToken = default);
}
