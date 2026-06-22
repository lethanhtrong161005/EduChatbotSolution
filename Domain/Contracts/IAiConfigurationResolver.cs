using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IAiConfigurationResolver
{
    Task<EffectiveAiConfiguration> GetAiConfigurationAsync(
        int subjectId,
        CancellationToken cancellationToken = default);
}
