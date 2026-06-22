using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Pgvector.EntityFrameworkCore;

namespace Business.Services.AI;

public class VectorSearchService(
    IUnitOfWork unitOfWork)
    : IVectorSearchService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IReadOnlyList<ChunkRetrieval>> SimilaritySearchCosineDistance(
        ReadOnlyMemory<float> embedding,
        int topK,
        double similarityThreshold,
        IReadOnlyList<int> allowedSubjects,
        CancellationToken cxlTkn)
    {
        return
            [.. await _unitOfWork.Chunks.GetAsync(
                preFilter: e => allowedSubjects.Contains(e.Document.Chapter.SubjectId),
                projection: e => new ChunkRetrieval
                {
                    ChunkId = e.Id,
                    ChunkText = e.ChunkText,
                    SimilarityScore = 1 - e.Embedding!.CosineDistance(new Pgvector.Vector(embedding))
                },
                postFilter: x => x.SimilarityScore >= similarityThreshold,
                orderBy: q => q.OrderByDescending(e => e.SimilarityScore),
                paginationSettings: (pageSize: topK, pageIndex: 1),
                cancellationToken: cxlTkn)];
    }
}
