using Domain.Common;
using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IDocumentIndexingCoordinator
{
    [Retry(Retries = 2)]
    Task IndexAsync(Guid documentId, CancellationToken cancellationToken = default);

    [Retry(Retries = 2)]
    Task IndexAsync(Guid documentId, EffectiveAiConfiguration configuration, CancellationToken cancellationToken = default);
}
