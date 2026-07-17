using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Business.Services.Account;

public class UserImportCoordinator(
    IUserManagementService userManagementService,
    IUserImportFileService importFileService,
    IUnitOfWork unitOfWork,
    IResourceRealtimeNotifier notifier,
    ILogger<UserImportCoordinator> logger)
    : IUserImportCoordinator
{
    private readonly IUserManagementService _userManagementService = userManagementService;
    private readonly IUserImportFileService _importFileService = importFileService;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IResourceRealtimeNotifier _notifier = notifier;
    private readonly ILogger<UserImportCoordinator> _logger = logger;

    public async Task ImportAsync(Guid batchId, CancellationToken cxlTkn)
    {
        var batch = (await _unitOfWork.UserImportBatches.GetAsync(
            includeProperties: [nameof(UserImportBatch.Rows)],
            filter: e => e.Id == batchId,
            cancellationToken: cxlTkn))
            .FirstOrDefault()
            ?? throw new EntityNotFoundException("Could not find target batch.");

        var rows = batch.Rows.OrderBy(e => e.RowNumber).ToList();

        try
        {
            if (IsComplete(batch))
            {
                if (batch.Status != ImportBatchStatus.Completed)
                {
                    await SaveAndPushUpdate(batch, ImportBatchStatus.Completed, cxlTkn: cxlTkn);
                }
                return;
            }

            batch.ErrorMessage = null;
            if (rows.Count == 0) rows = await ParseAsync(batch, cxlTkn);
            await ProcessRowsAsync(batch, rows, cxlTkn);

            batch.ErrorMessage = null;
            batch.CompletedAt = DateTimeOffset.UtcNow;
            var terminalStatus = batch.FailedRows == 0 ? ImportBatchStatus.Completed : (batch.SuccessRows > 0 ? ImportBatchStatus.PartiallyCompleted : ImportBatchStatus.Failed);
            var properties = new Dictionary<string, object?>
            {
                { nameof(UserImportBatch.ProcessedRows), batch.ProcessedRows },
                { nameof(UserImportBatch.SuccessRows), batch.SuccessRows },
                { nameof(UserImportBatch.FailedRows), batch.FailedRows },
                { nameof(UserImportBatch.TotalRows), batch.TotalRows },
            };
            await SaveAndPushUpdate(batch, terminalStatus, properties, cxlTkn: cxlTkn);
        }
        catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await SaveFailure(batch, ex);
            throw;
        }
    }

    private static bool IsComplete(UserImportBatch batch) =>
        batch.Rows.Count > 0
        && batch.Rows.Count == batch.TotalRows
        && batch.ProcessedRows == batch.TotalRows
        && batch.Rows.All(e => e.Status == ImportRowStatus.Success);

    private async Task<List<UserImportRow>> ParseAsync(UserImportBatch batch, CancellationToken cxlTkn)
    {
        batch.ProcessedRows = 0;
        batch.SuccessRows = 0;
        batch.FailedRows = 0;
        batch.TotalRows = 0;
        await SaveAndPushUpdate(batch, ImportBatchStatus.Parsing, cxlTkn: cxlTkn);

        var readResult = await _importFileService.OpenReadAsync(batch.Id, cxlTkn);
        if (!readResult.Success) throw new FileNotFoundException($"Failed to download file from storage: {string.Join(", ", readResult.Errors)}");

        await using var stream = readResult.FileStream;
        var validationResult = await _userManagementService.ParseImportBatchAsync(stream, cxlTkn);
        if (!validationResult.IsValid) throw new InvalidOperationException($"Failed to parse file: {string.Join(" | ", validationResult.Errors)}");

        var rows = validationResult.ValidRows.OrderBy(e => e.RowNumber).ToList();
        if (rows.Count == 0) throw new InvalidOperationException("The parser produce no user rows.");

        foreach (var row in rows)
        {
            row.BatchId = batch.Id;
            batch.Rows.Add(row);
            _unitOfWork.UserImportRows.Insert(row);
        }
        batch.TotalRows = rows.Count;

        await SaveAndPushUpdate(batch, ImportBatchStatus.Validated, cxlTkn: cxlTkn);
        return rows;
    }

    private async Task ProcessRowsAsync(UserImportBatch batch, List<UserImportRow> rows, CancellationToken cxlTkn = default)
    {
        batch.SuccessRows = rows.Count(e => e.Status == ImportRowStatus.Success);
        batch.FailedRows = rows.Count(e => e.Status == ImportRowStatus.Failed);
        batch.ProcessedRows = batch.SuccessRows + batch.FailedRows;

        var rowsToProcess = rows.Where(e => e.Status != ImportRowStatus.Success).ToList();
        if (rowsToProcess.Count == 0) return;

        await SaveAndPushUpdate(batch, ImportBatchStatus.Processing, cxlTkn: cxlTkn);

        foreach (var row in rowsToProcess)
        {
            cxlTkn.ThrowIfCancellationRequested();

            var oldStatus = row.Status;
            var processed = await ProcessRowAsync(row, cxlTkn);

            processed.ProcessedAt = DateTimeOffset.UtcNow;

            if (oldStatus == ImportRowStatus.Failed) batch.FailedRows--;
            if (processed.Status == ImportRowStatus.Success) batch.SuccessRows++;
            else batch.FailedRows++;
            batch.ProcessedRows = batch.SuccessRows + batch.FailedRows;

            var properties = new Dictionary<string, object?>
            {
                { nameof(UserImportBatch.ProcessedRows), batch.ProcessedRows },
                { nameof(UserImportBatch.SuccessRows), batch.SuccessRows },
                { nameof(UserImportBatch.FailedRows), batch.FailedRows },
                { nameof(UserImportBatch.TotalRows), batch.TotalRows },
                { "percentage", batch.TotalRows > 0 ? (int)(100d * batch.ProcessedRows / batch.TotalRows ) : 100 },
                { "lastRowEmail", row.Email },
                { "lastRowStatus", row.Status.ToString() },
                { "lastRowError", row.ErrorMessage },
            };
            await SaveAndPushUpdate(batch, ImportBatchStatus.Processing, properties, cxlTkn: cxlTkn);
        }
    }

    private async Task<UserImportRow> ProcessRowAsync(UserImportRow row, CancellationToken cxlTkn)
    {
        try
        {
            var dto = new CreateUserDto(row.FullName, row.Email, row.Role);
            var (success, user, error) = await _userManagementService.CreateUserAsync(dto);

            if (success)
            {
                row.Status = ImportRowStatus.Success;
                row.CreatedUserId = user!.Id;
                row.ErrorMessage = null;
            }
            else
            {
                row.Status = ImportRowStatus.Failed;
                row.ErrorMessage = error;
            }
        }
        catch (OperationCanceledException) when (cxlTkn.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            row.Status = ImportRowStatus.Failed;
            row.ErrorMessage = ex.Message;
        }

        return row;
    }

    private async Task SaveAndPushUpdate(
        UserImportBatch batch,
        ImportBatchStatus status,
        Dictionary<string, object?>? properties = null,
        string? error = null,
        CancellationToken cxlTkn = default)
    {
        batch.Status = status;
        await _unitOfWork.SaveAsync(cxlTkn);

        try
        {
            properties ??= [];
            properties[nameof(UserImportBatch.Status)] = batch.Status.ToString();
            if (error != null) properties["error"] = error;

            var isTerminal = status is ImportBatchStatus.Completed or ImportBatchStatus.PartiallyCompleted or ImportBatchStatus.Failed;
            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.ImportBatch,
                Action = isTerminal ? ResourceAction.Updated : ResourceAction.ProgressUpdated,
                ResourceId = batch.Id.ToString(),
                Properties = properties,
            };
            await _notifier.PushUpdateAsync(update);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not publish import status for user batch {BatchId}.", batch.Id);
        }
    }

    private async Task SaveFailure(UserImportBatch batch, Exception exception)
    {
        batch.ErrorMessage = exception.ToString();

        try
        {
            await SaveAndPushUpdate(batch, ImportBatchStatus.Failed, error: batch.ErrorMessage, cxlTkn: CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not persist import failure for user batch {BatchId}.", batch.Id);
        }
    }
}
