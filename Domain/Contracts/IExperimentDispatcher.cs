namespace Domain.Contracts;

public interface IExperimentDispatcher
{
    string Enqueue(Guid experimentId);
}
