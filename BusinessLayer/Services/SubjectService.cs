using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Entities;

namespace Business.Services;

public class SubjectService(IUnitOfWork unitOfWork) : ISubjectService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<IEnumerable<Subject>> GetAsync(CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Subjects.GetAsync(
            includeProperties:
            [
                nameof(Subject.Chapters) + "." + nameof(Chapter.Documents),
                nameof(Subject.AiConfiguration),
            ],
            cancellationToken: cxlTkn);
    }

    public async Task<Subject?> GetByIdAsync(int id, CancellationToken cxlTkn = default)
    {
        return (await _unitOfWork.Subjects.GetAsync(
            filter: e => e.Id == id,
            includeProperties:
            [
                nameof(Subject.Chapters) + "." + nameof(Chapter.Documents),
                nameof(Subject.AiConfiguration),
            ],
            cancellationToken: cxlTkn))
            .FirstOrDefault();
    }

    public async Task<Subject?> CreateAsync(Subject entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Subjects.Insert(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Subject?> UpdateAsync(Subject entity, CancellationToken cxlTkn = default)
    {
        var updatedEntity = _unitOfWork.Subjects.Update(entity);
        await _unitOfWork.SaveAsync(cxlTkn);
        return updatedEntity;
    }

    public async Task<Subject?> DeleteAsync(int id, CancellationToken cxlTkn = default)
    {
        var deletedEntity = await _unitOfWork.Subjects.DeleteAsync(id, cxlTkn);
        await _unitOfWork.SaveAsync(cxlTkn);
        return deletedEntity;
    }

    public async Task<IEnumerable<Subject>> GetAccessibleSubjectsAsync(Guid userId, CancellationToken cxlTkn = default)
    {
        return await _unitOfWork.Subjects.GetAsync(
            filter: e => e.Memberships.Any(d => d.UserId == userId),
            orderBy: e => e.OrderBy(e => e.Code),
            cancellationToken: cxlTkn);
    }

    public async Task<bool> IsChiefAsync(int subjectId, Guid userId, CancellationToken cxlTkn = default)
    {
        return (await _unitOfWork.SubjectMemberships.GetAsync(
            filter: e => e.SubjectId == subjectId
                         && e.UserId == userId
                         && e.Role == MembershipRole.Chief,
            cancellationToken: cxlTkn))
            .Any();
    }
}
