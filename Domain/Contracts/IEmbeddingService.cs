using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IEmbeddingService
{
    Task<EmbedResult> EmbedAsync(
        IEnumerable<string> texts,
        string modelName,
        CancellationToken cancellationToken = default);
}
