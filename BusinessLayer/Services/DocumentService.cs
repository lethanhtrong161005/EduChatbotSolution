using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;

namespace Business.Services;

public class DocumentService(IUnitOfWork unitOfWork) : IDocumentService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Document>> GetAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Documents.GetAsync(
            includeProperties:
            [
                nameof(Document.Chapter),
                nameof(Document.Uploader),
                nameof(Document.Comments)
            ],
            cancellationToken: cxlTkn);
    }

    public async Task<Document?> GetByIdAsync(Guid id, CancellationToken cxlTkn = default)
    {
        return (await _unitOfWork.Documents.GetAsync(
            filter: e => e.Id == id,
            includeProperties:
            [
                nameof(Document.Chapter),
                nameof(Document.Uploader),
                nameof(Document.Comments)
            ],
            cancellationToken: cxlTkn))
            .FirstOrDefault();
    }

    public async Task<Document?> CreateAsync(Document entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Documents.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Document?> UpdateAsync(Document entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Documents.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Document?> DeleteAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.Documents.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }

    public async Task<IEnumerable<Document>> CreateRange(IEnumerable<Document> entities, CancellationToken cancellationToken = default)
    {
        var insertedEntities = new List<Document>();
        foreach (var entity in entities)
        {
            insertedEntities.Add(_unitOfWork.Documents.Insert(entity));
        }
        await _unitOfWork.SaveAsync(cancellationToken);
        return insertedEntities;
    }

    public async Task<IEnumerable<Document>> UpdateRange(IEnumerable<Document> entities, CancellationToken cancellationToken = default)
    {
        var updatedEntities = new List<Document>();
        foreach (var entity in entities)
        {
            updatedEntities.Add(_unitOfWork.Documents.Update(entity));
        }
        await _unitOfWork.SaveAsync(cancellationToken);
        return updatedEntities;
    }

    public async Task<IEnumerable<Document>> DeleteRange(IEnumerable<Document> entities, CancellationToken cancellationToken = default)
    {
        var deletedEntities = new List<Document>();
        foreach (var entity in entities)
        {
            deletedEntities.Add(_unitOfWork.Documents.Delete(entity));
        }
        await _unitOfWork.SaveAsync(cancellationToken);
        return deletedEntities;
    }

    public async Task<IEnumerable<Document>> GetBySubjectAsync(int subjectId, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Documents.GetAsync(
            filter: e => e.Chapter.SubjectId == subjectId,
            includeProperties: [nameof(Document.Uploader)],
            cancellationToken: cxlTkn);
    }

    public async Task<IEnumerable<Document>> GetByChapterAsync(int chapterId, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Documents.GetAsync(
            filter: e => e.ChapterId == chapterId,
            includeProperties: [nameof(Document.Uploader)],
            cancellationToken: cxlTkn);
    }

    public async Task<IEnumerable<Chunk>> GetChunksAsync(
        Guid documentId,
        int pageSize = 10, int pageIndex = 1,
        CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Chunks.GetAsync(filter: c => c.DocumentId == documentId,
                                                 orderBy: q => q.OrderBy(c => c.ChunkIndex),
                                                 paginationSettings: (pageSize, pageIndex),
                                                 noTracking: true,
                                                 cancellationToken: cxlTkn);
    }

    public async Task<IEnumerable<Document>> GetDocumentsByChapterAsync(int chapterId)
    {

        return await _unitOfWork.Documents.GetAsync(
            filter: d => d.ChapterId == chapterId
        );
    }

    public async Task<Document?> GetDocumentWithCommentsAsync(Guid documentId)
    {

        var result = await _unitOfWork.Documents.GetAsync(
            filter: d => d.Id == documentId,
            includeProperties: new string[] { "Comments", "Comments.User" }
        );

        return result.FirstOrDefault();
    }

    public async Task<DocumentComment> AddCommentAsync(Guid documentId, Guid userId, string content)
    {
        var comment = new DocumentComment
        {
            DocumentId = documentId,
            UserId = userId,
            Content = content
        };

        await _unitOfWork.DocumentComments.InsertAsync(comment);

        await _unitOfWork.SaveAsync();

        return comment;
    }
}