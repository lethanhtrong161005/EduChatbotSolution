using DataAccess.Data;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess.Repositories;

public class ExperimentEvaluationRepository(EduChatAiDbContext context)
{
    private readonly EduChatAiDbContext _context = context;

    public virtual Task<TestResponse?> GetInputAsync(Guid testResponseId, CancellationToken cxlTkn = default) =>
        _context.TestResponses.Include(e => e.Experiment).ThenInclude(e => e.ConfigurationSnapshot).Include(e => e.RetrievedContexts).AsSplitQuery().SingleOrDefaultAsync(e => e.Id == testResponseId, cxlTkn);

    public virtual async Task<TestResponseEvaluationAttempt> AppendAttemptAsync(Guid testResponseId, TestResponseEvaluationAttempt attempt, CancellationToken cxlTkn = default)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        await using var transaction = await _context.Database.BeginTransactionAsync(cxlTkn);
        await LockResponseAsync(testResponseId, cxlTkn);

        attempt.TestResponseId = testResponseId;
        attempt.AttemptNumber = await _context.TestResponseEvaluationAttempts.Where(e => e.TestResponseId == testResponseId).Select(e => (int?)e.AttemptNumber).MaxAsync(cxlTkn) is { } last ? last + 1 : 1;
        _context.TestResponseEvaluationAttempts.Add(attempt);
        await _context.SaveChangesAsync(cxlTkn);
        await transaction.CommitAsync(cxlTkn);
        return attempt;
    }

    public virtual async Task SelectCurrentAsync(Guid testResponseId, Guid attemptId, CancellationToken cxlTkn = default)
    {
        var updated = await _context.TestResponses.Where(e => e.Id == testResponseId && e.EvaluationAttempts.Any(attempt => attempt.Id == attemptId && attempt.Status != EvaluationAttemptStatus.Pending && attempt.Status != EvaluationAttemptStatus.Running))
            .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.CurrentEvaluationAttemptId, attemptId), cxlTkn);
        if (updated == 0) throw new EntityConflictException("Only a terminal evaluation attempt belonging to the response can be selected.", nameof(TestResponse.CurrentEvaluationAttemptId));
    }

    private async Task LockResponseAsync(Guid testResponseId, CancellationToken cxlTkn)
    {
        await using var command = _context.Database.GetDbConnection().CreateCommand();
        command.Transaction = _context.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT id FROM test_responses WHERE id = @testResponseId FOR UPDATE";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "testResponseId";
        parameter.Value = testResponseId;
        command.Parameters.Add(parameter);
        if (await command.ExecuteScalarAsync(cxlTkn) is null or DBNull) throw new EntityNotFoundException(testResponseId);
    }
}
