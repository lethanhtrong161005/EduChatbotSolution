using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IExperimentService
{
    Task<ExperimentCreateOptionsDto?> GetCreateOptionsAsync(int subjectId, CancellationToken cancellationToken = default);
    Task<ExperimentIndexPreflightDto?> PreflightAsync(ExperimentIndexPreflightRequest request, CancellationToken cancellationToken = default);
    Task<CreateExperimentResponse> CreateAsync(CreateExperimentRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExperimentSummaryDto>> GetSummariesAsync(CancellationToken cancellationToken = default);
    Task<ExperimentResultDto?> GetResultAsync(Guid experimentId, CancellationToken cancellationToken = default);
    Task<ExperimentComparisonDto?> CompareAsync(Guid leftExperimentId, Guid rightExperimentId, CancellationToken cancellationToken = default);
}
