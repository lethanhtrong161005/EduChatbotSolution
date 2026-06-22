using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IVectorSearchService
{
    Task<IReadOnlyList<ChunkRetrieval>> SimilaritySearchCosineDistance(
        ReadOnlyMemory<float> embedding,
        int topK,
        double similarityThreshold,
        IReadOnlyList<int> allowedSubjects,
        CancellationToken cancellationToken);
}
