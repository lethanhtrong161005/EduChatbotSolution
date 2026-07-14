using DataAccess.Data;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess.Repositories;

public sealed record ExperimentRunPreparation(bool RequiresReindex, int AffectedDocumentCount);

public class ExperimentRunRepository(EduChatAiDbContext context)
{
    private readonly EduChatAiDbContext _context = context;

    public virtual Task<int> CountAffectedDocumentsAsync(int subjectId, string chunkingStrategy, int chunkSize, int chunkOverlap, string embeddingModel, CancellationToken cxlTkn = default) =>
        _context.Documents.Where(
            e => e.SubjectId == subjectId &&
            (e.Status != DocumentStatus.Indexed
            || e.IndexedChunkingStrategy != chunkingStrategy
            || e.IndexedChunkSize != chunkSize
            || e.IndexedChunkOverlap != chunkOverlap
            || e.IndexedEmbeddingModel != embeddingModel))
        .CountAsync(cxlTkn);

    public virtual async Task<Experiment> CreateQueuedAsync(Experiment experiment, CancellationToken cxlTkn = default)
    {
        ArgumentNullException.ThrowIfNull(experiment);

        await using var transaction = await _context.Database.BeginTransactionAsync(cxlTkn);
        await LockSubjectAsync(experiment.SubjectId, cxlTkn);

        if (await _context.Experiments.AnyAsync(e => e.SubjectId == experiment.SubjectId && e.Status != ExperimentStatus.Completed && e.Status != ExperimentStatus.Failed, cxlTkn))
            throw new EntityConflictException("Another experiment is already active for this subject.", nameof(Experiment.SubjectId));

        _context.Experiments.Add(experiment);
        await _context.SaveChangesAsync(cxlTkn);
        await transaction.CommitAsync(cxlTkn);
        return experiment;
    }

    public virtual async Task<ExperimentRunPreparation?> PrepareAsync(Guid experimentId, SubjectAiConfiguration configuration, CancellationToken cxlTkn = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        await using var transaction = await _context.Database.BeginTransactionAsync(cxlTkn);

        var subjectId = await _context.Experiments.AsNoTracking().Where(e => e.Id == experimentId).Select(e => (int?)e.SubjectId).SingleOrDefaultAsync(cxlTkn)
            ?? throw new EntityNotFoundException(experimentId);

        await LockSubjectAsync(subjectId, cxlTkn);

        var experiment = await _context.Experiments.Include(e => e.ConfigurationSnapshot).SingleAsync(e => e.Id == experimentId, cxlTkn);
        if (experiment.Status != ExperimentStatus.Queued) return null;
        if (configuration.Id != subjectId) throw new EntityValidationException("The AI configuration subject does not match the experiment subject.", nameof(configuration.Id));

        if (await _context.Experiments.AnyAsync(e => e.SubjectId == subjectId && e.Id != experimentId && e.Status != ExperimentStatus.Completed && e.Status != ExperimentStatus.Failed, cxlTkn))
            throw new EntityConflictException("Another experiment is already active for this subject.", nameof(Experiment.SubjectId));

        var availability = await _context.Subjects.Where(e => e.Id == subjectId).Select(e => e.IndexAvailability).SingleAsync(cxlTkn);
        if (availability == SubjectIndexAvailability.Reindexing)
            throw new EntityConflictException("The subject is being reindexed.", nameof(Subject.IndexAvailability));

        var snapshot = experiment.ConfigurationSnapshot;
        var affected = await CountAffectedDocumentsAsync(subjectId, snapshot.ChunkingStrategy, snapshot.ChunkSize, snapshot.ChunkOverlap, snapshot.EmbeddingModel, cxlTkn);
        var requiresReindex = availability != SubjectIndexAvailability.Ready || affected > 0;

        await UpsertConfigurationAsync(configuration, cxlTkn);

        if (requiresReindex) await _context.Subjects.Where(e => e.Id == subjectId).ExecuteUpdateAsync(setters => setters.SetProperty(e => e.IndexAvailability, SubjectIndexAvailability.Reindexing), cxlTkn);

        experiment.AffectedDocumentCount = affected;
        experiment.IndexedDocumentCount = 0;
        experiment.Status = requiresReindex ? ExperimentStatus.PreparingIndex : ExperimentStatus.Running;

        await _context.SaveChangesAsync(cxlTkn);
        await transaction.CommitAsync(cxlTkn);
        return new ExperimentRunPreparation(requiresReindex, affected);
    }

    public virtual Task<Experiment?> GetForRunAsync(Guid experimentId, CancellationToken cxlTkn = default) =>
        _context.Experiments
            .Include(e => e.ConfigurationSnapshot)
            .Include(e => e.TestResponses).ThenInclude(e => e.TestQuestion)
            .Include(e => e.TestResponses).ThenInclude(e => e.RetrievedContexts)
            .AsSplitQuery()
            .SingleOrDefaultAsync(e => e.Id == experimentId, cxlTkn);

    private async Task UpsertConfigurationAsync(SubjectAiConfiguration source, CancellationToken cxlTkn)
    {
        var target = await _context.SubjectAiConfigurations.SingleOrDefaultAsync(e => e.Id == source.Id, cxlTkn);
        if (target == null)
        {
            _context.SubjectAiConfigurations.Add(source);
            return;
        }

        target.ChunkingStrategy = source.ChunkingStrategy;
        target.ChunkSize = source.ChunkSize;
        target.ChunkOverlap = source.ChunkOverlap;
        target.EmbeddingModel = source.EmbeddingModel;
        target.TopK = source.TopK;
        target.SimilarityThreshold = source.SimilarityThreshold;
        target.MaxContextChunks = source.MaxContextChunks;
        target.LlmModel = source.LlmModel;
        target.ChatTemperature = source.ChatTemperature;
        target.MaxHistoryMessages = source.MaxHistoryMessages;
        target.ChatPrompt = source.ChatPrompt;
        target.ContextPrompt = source.ContextPrompt;
        target.NoContextRetrievedPrompt = source.NoContextRetrievedPrompt;
        target.TitleTemperature = source.TitleTemperature;
        target.TitlePrompt = source.TitlePrompt;
        target.CitationExtractionTemperature = source.CitationExtractionTemperature;
        target.CitationExtractionPrompt = source.CitationExtractionPrompt;
    }

    private async Task LockSubjectAsync(int subjectId, CancellationToken cxlTkn)
    {
        await using var command = _context.Database.GetDbConnection().CreateCommand();
        command.Transaction = _context.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = "SELECT id FROM subjects WHERE id = @subjectId FOR UPDATE";

        var parameter = command.CreateParameter();
        parameter.ParameterName = "subjectId";
        parameter.Value = subjectId;
        command.Parameters.Add(parameter);

        if (await command.ExecuteScalarAsync(cxlTkn) is null or DBNull) throw new EntityNotFoundException(subjectId);
    }
}
