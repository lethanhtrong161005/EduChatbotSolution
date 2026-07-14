using DataAccess.Data;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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

    public virtual async Task<SubjectAiConfiguration> SaveConfigurationAsync(SubjectAiConfiguration configuration, CancellationToken cxlTkn = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        await using var transaction = await _context.Database.BeginTransactionAsync(cxlTkn);
        var availability = await LockSubjectAsync(configuration.Id, cxlTkn) ?? throw new EntityNotFoundException(configuration.Id);

        if (availability == SubjectIndexAvailability.Reindexing)
            throw new EntityConflictException("AI configuration cannot be changed while the subject is being reindexed.", nameof(Subject.IndexAvailability));

        var stored = await _context.SubjectAiConfigurations.FindAsync([configuration.Id], cxlTkn);
        if (stored == null)
        {
            stored = _context.SubjectAiConfigurations.Add(configuration).Entity;
        }
        else
        {
            var createdAt = stored.CreatedAt;
            var updatedAt = stored.UpdatedAt;
            _context.Entry(stored).CurrentValues.SetValues(configuration);
            stored.CreatedAt = createdAt;
            stored.UpdatedAt = updatedAt;
        }

        await _context.SaveChangesAsync(cxlTkn);
        await transaction.CommitAsync(cxlTkn);
        return stored;
    }

    private async Task<SubjectIndexAvailability?> LockSubjectAsync(int subjectId, CancellationToken cxlTkn)
    {
        await using var command = _context.Database.GetDbConnection().CreateCommand();
        command.Transaction = _context.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT index_availability FROM subjects WHERE id = @subjectId FOR UPDATE";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "subjectId";
        parameter.Value = subjectId;
        command.Parameters.Add(parameter);

        var value = await command.ExecuteScalarAsync(cxlTkn);
        return value is null or DBNull ? null : (SubjectIndexAvailability)Convert.ToInt32(value);
    }
}
