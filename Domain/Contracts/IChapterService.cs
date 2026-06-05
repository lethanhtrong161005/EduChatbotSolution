using Domain.Entities;

namespace Domain.Contracts;

public interface IChapterService
{
    Task<IEnumerable<Chapter>> GetAsync(CancellationToken cancellationToken = default);
    Task<Chapter?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Chapter?> CreateAsync(Chapter entity, CancellationToken cancellationToken = default);
    Task<Chapter?> UpdateAsync(Chapter entity, CancellationToken cancellationToken = default);
    Task<Chapter?> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Chapter>> GetBySubjectAsync(int subjectId, CancellationToken cancellationToken = default);
}
