using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface ISubjectReindexCoordinator
{
    Task RunAsync(int subjectId, EffectiveAiConfiguration configuration, CancellationToken cancellationToken = default);
}
