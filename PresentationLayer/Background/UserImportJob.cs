using Domain.Common;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Presentation.Realtime;
using DataAccess.Data;
using System.Text.Json;

using Presentation.Background;
using Hangfire;

namespace Presentation.Background;

public sealed class UserImportJob(
    IServiceProvider serviceProvider,
    EduChatAiDbContext dbContext,
    IHubContext<ResourceHub, IResourceClient> hubContext,
    ILogger<UserImportJob> logger,
    IUserManagementService userManagementService,
    [Microsoft.Extensions.DependencyInjection.FromKeyedServices(DocumentStorageMethod.Supabase)] IDurableStorageStrategy storageStrategy,
    IBackgroundJobClient backgroundJobs)
{
    [Queue(HangfireConstants.MediumPriorityQueue)]
    [Retry(Retries = 1)]
    public async Task ParseAsync(Guid batchId, CancellationToken cxlTkn = default)
    {
        var batch = await dbContext.UserImportBatches.FindAsync([batchId], cxlTkn);
        if (batch == null || batch.Status != ImportBatchStatus.Pending) return;

        batch.Status = ImportBatchStatus.Parsing;
        await dbContext.SaveChangesAsync(cxlTkn);
        await PushProgressAsync(batchId, ImportBatchStatus.Parsing.ToString());

        try
        {
            var readResult = await storageStrategy.OpenReadAsync(batch.StorageLocator, cxlTkn);
            if (!readResult.Success || readResult.FileStream == null)
            {
                throw new Exception($"Failed to download file from storage: {string.Join(", ", readResult.Errors ?? Array.Empty<string>())}");
            }
            
            using var ms = new MemoryStream();
            await readResult.FileStream.CopyToAsync(ms, cxlTkn);
            ms.Position = 0; // reset to beginning before passing to parser
            readResult.FileStream.Dispose();

            var validationResult = await userManagementService.ParseAndValidateImportBatchAsync(batchId, ms);

            // Re-fetch batch to get the updated status and row counts
            batch = await dbContext.UserImportBatches.FindAsync([batchId], cxlTkn);
            
            if (!validationResult.IsValid)
            {
                batch!.Status = ImportBatchStatus.Failed;
                batch.ErrorMessage = string.Join(" | ", validationResult.Errors);
                await dbContext.SaveChangesAsync(cxlTkn);
                
                await PushProgressAsync(batchId, ImportBatchStatus.Failed.ToString(), batch.ErrorMessage);
                return;
            }

            batch!.Status = ImportBatchStatus.Validated;
            await dbContext.SaveChangesAsync(cxlTkn);
            
            await PushProgressAsync(batchId, ImportBatchStatus.Validated.ToString());

            // Enqueue Phase 2
            backgroundJobs.Enqueue<UserImportJob>(j => j.ProcessRowsAsync(batchId, CancellationToken.None));
        }
        catch (Exception ex)
        {
            batch = await dbContext.UserImportBatches.FindAsync([batchId], cxlTkn);
            if (batch != null)
            {
                batch.Status = ImportBatchStatus.Failed;
                batch.ErrorMessage = "Failed to parse file: " + ex.Message;
                await dbContext.SaveChangesAsync(cxlTkn);
                await PushProgressAsync(batchId, ImportBatchStatus.Failed.ToString(), batch.ErrorMessage);
            }
        }
    }

    [Queue(HangfireConstants.MediumPriorityQueue)]
    [Retry(Retries = 0)]
    public async Task ProcessRowsAsync(Guid batchId, CancellationToken cxlTkn = default)
    {
        var batch = await dbContext.UserImportBatches
            .Include(b => b.Rows)
            .FirstOrDefaultAsync(b => b.Id == batchId, cxlTkn);

        if (batch == null || batch.Status != ImportBatchStatus.Validated) return;

        batch.Status = ImportBatchStatus.Processing;
        await dbContext.SaveChangesAsync(cxlTkn);

        var rowsToProcess = batch.Rows.Where(r => r.Status == ImportRowStatus.Pending).OrderBy(r => r.RowNumber).ToList();

        foreach (var row in rowsToProcess)
        {
            if (cxlTkn.IsCancellationRequested) break;

            await userManagementService.ProcessImportBatchRowAsync(batch.Id, row.Id);

            // Reload batch counts
            batch = await dbContext.UserImportBatches.Include(b => b.Rows).FirstAsync(b => b.Id == batchId, cxlTkn);
            
            var properties = new Dictionary<string, object?>
            {
                { "processedRows", batch.ProcessedRows },
                { "totalRows", batch.TotalRows },
                { "successRows", batch.SuccessRows },
                { "failedRows", batch.FailedRows },
                { "percentage", batch.TotalRows > 0 ? (int)((batch.ProcessedRows / (double)batch.TotalRows) * 100) : 100 },
                { "lastRowEmail", row.Email },
                { "lastRowStatus", row.Status.ToString() },
                { "lastRowError", row.ErrorMessage }
            };

            var update = new ResourceUpdate
            {
                ResourceType = ResourceType.ImportBatch,
                Action = ResourceAction.ProgressUpdated,
                ResourceId = batch.Id.ToString(),
                Properties = properties
            };

            await hubContext.Clients.Group(HubGroups.Resource("import-batch")).ResourceChanged(update);
        }

        batch.Status = batch.FailedRows > 0 ? ImportBatchStatus.PartiallyCompleted : ImportBatchStatus.Completed;
        batch.CompletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cxlTkn);

        var finalUpdate = new ResourceUpdate
        {
            ResourceType = ResourceType.ImportBatch,
            Action = ResourceAction.Updated,
            ResourceId = batch.Id.ToString(),
            Properties = new Dictionary<string, object?>
            {
                { "status", batch.Status.ToString() },
                { "processedRows", batch.ProcessedRows },
                { "successRows", batch.SuccessRows },
                { "failedRows", batch.FailedRows }
            }
        };
        await hubContext.Clients.Group(HubGroups.Resource("import-batch")).ResourceChanged(finalUpdate);
    }

    private async Task PushProgressAsync(Guid batchId, string status, string? error = null)
    {
        var properties = new Dictionary<string, object?> { { "status", status } };
        if (error != null) properties.Add("error", error);

        var update = new ResourceUpdate
        {
            ResourceType = ResourceType.ImportBatch,
            Action = ResourceAction.ProgressUpdated,
            ResourceId = batchId.ToString(),
            Properties = properties
        };
        await hubContext.Clients.Group(HubGroups.Resource("import-batch")).ResourceChanged(update);
    }
}
