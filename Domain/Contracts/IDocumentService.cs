using Domain.Entities;

namespace Domain.Contracts;

public interface IDocumentService
{
    Task<IEnumerable<Document>> GetAsync(CancellationToken cancellationToken = default);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Document?> CreateAsync(Document entity, CancellationToken cancellationToken = default);
    Task<Document?> UpdateAsync(Document entity, CancellationToken cancellationToken = default);
    Task<Document?> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Document>> CreateRange(IEnumerable<Document> entities, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> UpdateRange(IEnumerable<Document> entities, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> DeleteRange(IEnumerable<Document> entities, CancellationToken cancellationToken = default);

    Task<IEnumerable<Document>> GetBySubjectAsync(int subjectid, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> GetByChapterAsync(int chapterId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Chunk>> GetChunksAsync(Guid documentId, int pageSize = 10, int pageIndex = 1, CancellationToken cxlTkn = default);
}
