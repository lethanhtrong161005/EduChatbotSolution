using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Utils;
using Microsoft.AspNetCore.Identity;

namespace Business.Services.Documents;

public class DocumentService(
    UserManager<ApplicationUser> userManager,
    IUnitOfWork unitOfWork)
    : IDocumentService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Document>> GetAsync(string[] inclProps = null!, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Documents.GetAsync(
            includeProperties: inclProps ?? [],
            cancellationToken: cxlTkn);
    }

    public async Task<Document?> GetByIdAsync(Guid id, string[] inclProps = null!, CancellationToken cxlTkn = default)
    {
        return (await _unitOfWork.Documents.GetAsync(
            filter: e => e.Id == id,
            includeProperties: inclProps ?? [],
            cancellationToken: cxlTkn))
            .FirstOrDefault();
    }

    public async Task<DocumentDetails?> GetDocumentDetailsByIdAsync(Guid id, CancellationToken cxlTkn = default)
    {
        var doc = (await _unitOfWork.Documents.GetAsync(
            preFilter: e => e.Id == id,
            projection: e => new DocumentDetails
            {
                Id = e.Id,
                SubjectId = e.SubjectId,
                SubjectName = e.Subject.Name,
                Title = e.Title,
                Description = e.Description,
                FileName = e.FileName,
                OriginalFileName = e.OriginalFileName,
                FileType = e.FileType,
                FileSize = e.FileSize,
                Status = e.Status.ToString(),
                ParserUsed = e.ParserUsed,
                EmbeddingModel = e.Chunks.FirstOrDefault() != null ? e.Chunks.First().EmbeddingModel : null,
                ChunkCount = e.Chunks.Count,
                IndexingErrors = e.IndexingErrors,
                UploadedAt = e.UploadedAt,
                UploaderId = e.UploaderId,
                UploadedBy = e.Uploader.FullName,
                Chapters = e.Chapters.Select(x => new ChapterInfo
                {
                    Id = x.Id,
                    ChapterNumber = x.ChapterNumber,
                    Name = x.Name,
                }).ToList(),
                ParsedSections = e.ParsedSections.Select(x => new ParsedSectionDetails
                {
                    SectionIndex = x.SectionIndex,
                    Text = x.Text,
                    LocationInDocument = x.BuildLocation(),
                }).ToList(),
            },
            asNoTracking: true,
            asSplitQuery: true,
            cancellationToken: cxlTkn))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("No document matched the provided ID.");

        var comments = await _unitOfWork.DocumentComments.GetAsync(
            filter: e => e.DocumentId == doc.Id,
            includeProperties: [nameof(DocumentComment.Author)],
            asNoTracking: true,
            cancellationToken: cxlTkn);

        var userIds = comments.Select(e => e.AuthorId).ToHashSet();

        var userRoles = await _unitOfWork.UserRoles.GetAsync(
            preFilter: e => userIds.Contains(e.UserId),
            projection: e => new UserRoleLookup { UserId = e.UserId, Role = e.Role.Name ?? string.Empty },
            asNoTracking: true,
            cancellationToken: cxlTkn);

        var hasLecturer = userRoles.Any(e => e.Role == nameof(UserRole.Lecturer));
        if (hasLecturer)
        {
            var chiefId = (await _unitOfWork.Memberships.GetAsync(
                preFilter: e => e.SubjectId == doc.SubjectId && e.Role == MembershipRole.Chief,
                projection: e => new { e.UserId },
                cancellationToken: cxlTkn))
                .FirstOrDefault()
                ?.UserId;

            userRoles.FirstOrDefault(e => e.UserId == chiefId)?.Role = nameof(MembershipRole.Chief);
        }

        var userRoleDict = userRoles.ToDictionary(e => e.UserId, e => e.Role);

        var commentDtos = comments.Select(e => new DocumentCommentDetails
        {
            AuthorId = e.AuthorId,
            AuthorName = e.Author.FullName,
            AuthorRole = userRoleDict.GetValueOrDefault(e.AuthorId, string.Empty),
            Content = e.Content,
            CreatedAt = e.CreatedAt,
        });

        doc.Comments = [.. commentDtos];

        return doc;
    }

    private record UserRoleLookup
    {
        public Guid UserId { get; set; }

        public string Role { get; set; } = string.Empty;
    }

    public async Task<IEnumerable<Document>> GetBySubjectAsync(int subjectId, string[] inclProps = null!, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Documents.GetAsync(
            filter: e => e.SubjectId == subjectId,
            includeProperties: inclProps ?? [],
            cancellationToken: cxlTkn);
    }

    public async Task<IEnumerable<Document>> GetByChapterAsync(int chapterId, string[] inclProps = null!, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Documents.GetAsync(
            filter: e => e.DocumentChapters.Select(x => x.ChapterId).Contains(chapterId),
            includeProperties: inclProps ?? [],
            cancellationToken: cxlTkn);
    }

    public async Task<IEnumerable<Document>> GetByUploaderAsync(Guid uploaderId, string[] inclProps = null!, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Documents.GetAsync(
            filter: e => e.UploaderId == uploaderId,
            includeProperties: inclProps ?? [],
            cancellationToken: cxlTkn);
    }

    public async Task<Document> CreateAsync(Document entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Documents.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Document> UpdateAsync(Document entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Documents.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Document> DeleteAsync(Guid id, CancellationToken cxlTkn = default)
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

    public async Task<IEnumerable<Chapter>> GetChaptersOfDocumentAsync(Guid documentId, CancellationToken cxlTkn = default)
    {
        var doc = (await _unitOfWork.Documents.GetAsync(
            filter: e => e.Id == documentId,
            includeProperties: [nameof(Document.Chapters)],
            asNoTracking: true,
            cancellationToken: cxlTkn))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("No document matched the provided ID.");

        return doc.Chapters;
    }

    public async Task<Document> AddChaptersToDocumentAsync(Guid documentId, IEnumerable<int> chapterIds, CancellationToken cxlTkn = default)
    {
        // 1. Verify the document exists
        var doc = (await _unitOfWork.Documents.GetAsync(
            includeProperties: [nameof(Document.DocumentChapters)],
            filter: e => e.Id == documentId,
            cancellationToken: cxlTkn))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("No document matched the provided ID.");

        // 2. Compute the chapters to add, lenient on duplicates
        var @new = chapterIds.ToHashSet();
        var existing = doc.DocumentChapters.Select(dc => dc.ChapterId).ToHashSet();
        var toAdd = @new.Except(existing).ToList();

        if (toAdd.Count == 0)
            return doc;

        // 3. Verify the chapters exist and belong to the same subject as the document
        var chapterIdsToAdd = (await _unitOfWork.Chapters.GetAsync(
            preFilter: e => toAdd.Contains(e.Id) && e.SubjectId == doc.SubjectId,
            projection: e => new { e.Id },
            asNoTracking: true,
            cancellationToken: cxlTkn))
            .ToList();

        if (chapterIdsToAdd.Count != toAdd.Count)
        {
            var missingChapterIds = toAdd.Except(chapterIdsToAdd.Select(x => x.Id));
            throw new EntityNotFoundException(
                $"One or more chapters do not exist or do not belong to the same subject as the target document: " +
                $"{string.Join(", ", missingChapterIds.Select(x => x.ToString()))}");
        }

        // 4. Add the chapters to the document
        foreach (var chapterId in toAdd)
        {
            doc.DocumentChapters.Add(new DocumentChapter
            {
                DocumentId = documentId,
                ChapterId = chapterId,
                SubjectId = doc.SubjectId,
            });
        }

        await _unitOfWork.SaveAsync(cxlTkn);

        return doc;
    }

    public async Task<Document> RemoveChaptersFromDocumentAsync(Guid documentId, IEnumerable<int> chapterIds, CancellationToken cxlTkn = default)
    {
        // 1. Verify the document exists
        var doc = (await _unitOfWork.Documents.GetAsync(
            includeProperties: [nameof(Document.DocumentChapters)],
            filter: e => e.Id == documentId,
            cancellationToken: cxlTkn))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("No document matched the provided ID.");

        // 2. Compute the chapters to remove, lenient if some chapters are not associated
        var toRemove = chapterIds.ToHashSet();
        var documentChaptersToRemove = doc.DocumentChapters.Where(e => toRemove.Contains(e.ChapterId)).ToList();

        if (documentChaptersToRemove.Count == 0)
            return doc;

        // 3. Remove the chapters from the document
        foreach (var dc in documentChaptersToRemove)
        {
            doc.DocumentChapters.Remove(dc);
        }

        await _unitOfWork.SaveAsync(cxlTkn);

        return doc;
    }

    public async Task<IEnumerable<Chunk>> GetChunksAsync(
        Guid documentId,
        int pageSize = 10, int pageIndex = 1,
        CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Chunks.GetAsync(filter: c => c.DocumentId == documentId,
                                                 orderBy: q => q.OrderBy(c => c.ChunkIndex),
                                                 paginationSettings: (pageSize, pageIndex),
                                                 asNoTracking: true,
                                                 cancellationToken: cxlTkn);
    }

    public async Task<DocumentComment> AddCommentAsync(Guid documentId, Guid userId, string content)
    {
        var comment = new DocumentComment
        {
            DocumentId = documentId,
            AuthorId = userId,
            Content = content
        };

        await _unitOfWork.DocumentComments.InsertAsync(comment);
        await _unitOfWork.SaveAsync();
        return comment;
    }
}