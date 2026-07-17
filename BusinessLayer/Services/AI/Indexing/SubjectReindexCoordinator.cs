using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Business.Services.AI.Indexing;

public sealed class SubjectReindexCoordinator(
    IUnitOfWork unitOfWork,
    IDocumentIndexingCoordinator indexingCoordinator,
    ILogger<SubjectReindexCoordinator> logger)
    : ISubjectReindexCoordinator
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IDocumentIndexingCoordinator _indexingCoordinator = indexingCoordinator;
    private readonly ILogger<SubjectReindexCoordinator> _logger = logger;

    public async Task RunAsync(int subjectId, EffectiveAiConfiguration configuration, CancellationToken cxlTkn = default)
    {
        var subject = await _unitOfWork.Subjects.FindByIdAsync(subjectId, cxlTkn) ?? throw new EntityNotFoundException(subjectId);
        if (subject.IndexAvailability != SubjectIndexAvailability.Reindexing) return;

        try
        {
            var documents = (await _unitOfWork.Documents.GetAsync(
                filter: e => e.SubjectId == subjectId,
                orderBy: q => q.OrderBy(e => e.UploadedAt).ThenBy(e => e.Id),
                asNoTracking: true,
                cancellationToken: cxlTkn)).ToList();

            foreach (var document in documents)
            {
                cxlTkn.ThrowIfCancellationRequested();
                await _indexingCoordinator.IndexAsync(document.Id, configuration, cxlTkn);
            }

            await _unitOfWork.SubjectIndexes.SetAvailabilityAsync(subjectId, SubjectIndexAvailability.Ready, cxlTkn);
        }
        catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subject {SubjectId} reindexing failed.", subjectId);

            try
            {
                await _unitOfWork.SubjectIndexes.SetAvailabilityAsync(subjectId, SubjectIndexAvailability.Failed, CancellationToken.None);
            }
            catch (Exception stateException)
            {
                _logger.LogError(stateException, "Could not mark subject {SubjectId} index as failed.", subjectId);
            }

            throw;
        }
    }
}
