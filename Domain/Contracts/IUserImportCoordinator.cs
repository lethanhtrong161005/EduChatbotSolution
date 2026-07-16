namespace Domain.Contracts;

public interface IUserImportCoordinator
{
    Task ImportAsync(Guid batchId, CancellationToken cancellationToken);
}
