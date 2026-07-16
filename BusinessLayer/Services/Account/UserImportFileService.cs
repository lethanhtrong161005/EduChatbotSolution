using DataAccess.UnitOfWork;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.Account;

// TODO: Strategy selection
public class UserImportFileService(
    IStagingFileStore stagingStore,
    [FromKeyedServices(FileStorageMethod.LocalHardDrive)] IDurableStorageStrategy localHardDriveStrategy,
    [FromKeyedServices(FileStorageMethod.Supabase)] IDurableStorageStrategy supabaseStrategy,
    IUnitOfWork unitOfWork)
    : IUserImportFileService
{
    private readonly IStagingFileStore _stagingStore = stagingStore;
    private readonly IDurableStorageStrategy _localHardDriveStrategy = localHardDriveStrategy;
    private readonly IDurableStorageStrategy _supabaseStrategy = supabaseStrategy;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<FileStorageResult> PersistAsync(
        Guid batchId,
        CancellationToken cxlTkn = default)
    {
        var batch = await _unitOfWork.UserImportBatches.FindByIdAsync(batchId, cxlTkn);
        if (batch == null) return StoreFailure("No user import batch matched the provided ID");

        if (string.IsNullOrWhiteSpace(batch.StagingLocator))
            return StoreFailure("Batch has no staged file.");

        var readResult = await _stagingStore.OpenReadAsync(batch.StagingLocator, cxlTkn);
        if (!readResult.Success) return StoreFailure(readResult.Errors);

        FileStorageResult storeResult;
        await using (readResult.FileStream)
        {
            storeResult = await _supabaseStrategy.StoreAsync(
                readResult.FileStream,
                FileResourceType.UserImportBatch,
                GetStorageFileName(batch),
                FileDirectoryCategory.Received,
                cxlTkn);
        }

        if (!storeResult.Success) return StoreFailure(storeResult.Errors);

        var stagingLocator = batch.StagingLocator;
        batch.StorageLocator = storeResult.Locator;
        batch.StagingLocator = null;
        await _unitOfWork.SaveAsync(cxlTkn);

        await TryDeleteStagingAsync(stagingLocator);

        return new FileStorageResult
        {
            Success = true,
            Locator = batch.StorageLocator,
        };
    }

    public async Task<FileReadResult> OpenReadAsync(Guid batchId, CancellationToken cxlTkn)
    {
        var batch = await _unitOfWork.UserImportBatches.FindByIdAsync(batchId, cxlTkn);
        if (batch == null) return ReadFailure("No user import batch matched the provided ID");

        if (string.IsNullOrWhiteSpace(batch.StorageLocator))
            return ReadFailure("Batch has no durable storage locator.");

        return await _supabaseStrategy.OpenReadAsync(batch.StorageLocator, cxlTkn);
    }

    private async Task TryDeleteStagingAsync(string stagingLocator)
    {
        try
        {
            await _stagingStore.DeleteAsync(stagingLocator, CancellationToken.None);
        }
        catch (Exception)
        {
            // Do not abort on cleanup failure
            // Durable storage state is already committed
            // Scheduled cleanup remains the fallback
        }
    }

    private string GetStorageFileName(UserImportBatch batch) => $"{batch.Id}{Path.GetExtension(batch.FileName).ToLowerInvariant()}";

    private static FileStorageResult StoreFailure(string[] errors) => new() { Success = false, Errors = errors };
    private static FileStorageResult StoreFailure(string error) => new() { Success = false, Errors = [error] };
    private static FileReadResult ReadFailure(string error) => new() { Success = false, Errors = [error] };
}
