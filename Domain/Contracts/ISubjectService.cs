using Domain.Entities;

namespace Domain.Contracts;

public interface ISubjectService
{
    Task<IEnumerable<Subject>> GetAsync(CancellationToken cancellationToken = default);
    Task<Subject?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Subject?> CreateAsync(Subject entity, CancellationToken cancellationToken = default);
    Task<Subject?> UpdateAsync(Subject entity, CancellationToken cancellationToken = default);
    Task<Subject?> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Subject>> GetAccessibleSubjectsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsChiefAsync(int subjectId, Guid userId, CancellationToken cancellationToken = default);
}
