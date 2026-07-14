using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IDocumentIndexingCoordinator
{
    Task IndexAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task IndexAsync(Guid documentId, EffectiveAiConfiguration configuration, CancellationToken cancellationToken = default);
}
