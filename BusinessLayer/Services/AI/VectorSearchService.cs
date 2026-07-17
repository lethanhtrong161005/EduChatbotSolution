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
                preFilter: e => allowedSubjects.Contains(e.Document.SubjectId),
                projection: e => new ChunkRetrieval
                {
                    ChunkId = e.Id,
                    DocumentId = e.DocumentId,
                    SubjectId = e.Document.SubjectId,
                    ChunkIndex = e.ChunkIndex,
                    ChunkText = e.ChunkText,
                    SimilarityScore = 1 - e.Embedding!.CosineDistance(new Pgvector.Vector(embedding)),
                    DocumentTitle = e.Document.Title,
                    DocumentFileName = e.Document.OriginalFileName,
                    SubjectCode = e.Document.Subject.Code,
                    SubjectName = e.Document.Subject.Name,
                    StartPageNumber = e.StartPageNumber,
                    EndPageNumber = e.EndPageNumber,
                    StartSectionTitle = e.StartSectionTitle,
                    EndSectionTitle = e.EndSectionTitle,
                    ChunkingStrategy = e.ChunkingStrategy,
                    ChunkSize = e.ChunkSize,
                    ChunkOverlap = e.ChunkOverlap,
                    EmbeddingModel = e.EmbeddingModel,
                },
                postFilter: x => x.SimilarityScore >= similarityThreshold,
                orderBy: q => q.OrderByDescending(e => e.SimilarityScore),
                paginationSettings: (pageSize: topK, pageIndex: 1),
                cancellationToken: cxlTkn)];
    }
}
