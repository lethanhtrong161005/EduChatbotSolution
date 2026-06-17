using Domain.DTOs;

namespace Domain.Contracts;

public interface IEmbeddingService
{
    string ModelName { get; }

    Task<EmbeddingResult> EmbedAsync(IEnumerable<string> texts, CancellationToken cxlTkn = default);
}
