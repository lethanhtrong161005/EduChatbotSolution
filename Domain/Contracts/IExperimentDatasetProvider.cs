using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IExperimentDatasetProvider
{
    Task<TestDatasetDto> GetDatasetAsync(CancellationToken cancellationToken = default);
    Task<int> ImportAsync(CancellationToken cancellationToken = default);
}
