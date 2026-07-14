using DataAccess.Data;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class SubjectIndexRepository(EduChatAiDbContext context)
{
    private readonly EduChatAiDbContext _context = context;

    public virtual async Task<SubjectIndexAvailability> AcquireExclusiveReindexAsync(int subjectId, CancellationToken cxlTkn = default)
    {
        var current = await _context.Subjects.AsNoTracking().Where(e => e.Id == subjectId)
            .Select(e => (SubjectIndexAvailability?)e.IndexAvailability).SingleOrDefaultAsync(cxlTkn)
            ?? throw new EntityNotFoundException(subjectId);

        if (current == SubjectIndexAvailability.Reindexing) throw new EntityConflictException("The subject is already being reindexed.", nameof(Subject.IndexAvailability));

        var changed = await _context.Subjects.Where(e => e.Id == subjectId && e.IndexAvailability == current)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.IndexAvailability, SubjectIndexAvailability.Reindexing), cxlTkn);

        if (changed != 1) throw new EntityConflictException("The subject indexing state changed concurrently.", nameof(Subject.IndexAvailability));
        return current;
    }

    public virtual async Task SetAvailabilityAsync(int subjectId, SubjectIndexAvailability availability, CancellationToken cxlTkn = default)
    {
        var changed = await _context.Subjects.Where(e => e.Id == subjectId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.IndexAvailability, availability), cxlTkn);

        if (changed != 1) throw new EntityNotFoundException(subjectId);
    }
}
