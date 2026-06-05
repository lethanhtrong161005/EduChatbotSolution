using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;

namespace Business.Services;

public class ChapterService(IUnitOfWork unitOfWork) : IChapterService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Chapter>> GetAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Chapters.GetAsync(
            includeProperties: [nameof(Chapter.Subject), nameof(Chapter.Documents)],
            cancellationToken: cxlTkn);
    }

    public async Task<Chapter?> GetByIdAsync(int id, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Chapters.GetByIdAsync(id, cxlTkn);
    }

    public async Task<Chapter?> CreateAsync(Chapter entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Chapters.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Chapter?> UpdateAsync(Chapter entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Chapters.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Chapter?> DeleteAsync(int id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.Chapters.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }

    public async Task<IEnumerable<Chapter>> GetBySubjectAsync(int subjectId, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Chapters.GetAsync(
            filter: e => e.SubjectId == subjectId,
            cancellationToken: cxlTkn);
    }
}
