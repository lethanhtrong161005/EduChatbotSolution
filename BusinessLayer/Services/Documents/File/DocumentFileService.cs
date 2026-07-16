using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Domain.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Services.Documents.File;

public sealed class DocumentFileService(
    IDocumentService documentService,
    IDocumentStorageMethodResolver storageMethodResolver,
    IStagingFileStore stagingStore,
    [FromKeyedServices(FileStorageMethod.LocalHardDrive)] IDurableStorageStrategy localHardDriveStrategy,
    [FromKeyedServices(FileStorageMethod.Supabase)] IDurableStorageStrategy supabaseStrategy)
    : IDocumentFileService
{
    public async Task<bool> ExistsAsync(Guid documentId, CancellationToken cxlTkn = default)
    {
        var document = await GetDocumentAsync(documentId, cxlTkn);
        if (document == null) return false;

        if (document.StorageMethod == FileStorageMethod.Unspecified)
            return !string.IsNullOrWhiteSpace(document.StagingLocator)
                   && await stagingStore.ExistsAsync(document.StagingLocator, cxlTkn);

        var strategy = GetStrategy(document.StorageMethod);
        return strategy != null
               && !string.IsNullOrWhiteSpace(document.StorageLocator)
               && await strategy.ExistsAsync(document.StorageLocator, cxlTkn);
    }

    public async Task<FileStorageResult> PersistAsync(
        Guid documentId,
        CancellationToken cxlTkn = default)
    {
        var document = await GetDocumentAsync(documentId, cxlTkn);
        if (document == null) return StoreFailure("No document matched the provided ID.");

        if (string.IsNullOrWhiteSpace(document.StagingLocator))
            return StoreFailure("Document has no staged file.");

        var storageMethod = await storageMethodResolver.ResolveForPersistenceAsync(document, cxlTkn);

        var strategy = GetStrategy(storageMethod);
        if (strategy == null)
            return StoreFailure($"Unsupported document storage method '{storageMethod}'.");

        var readResult = await stagingStore.OpenReadAsync(document.StagingLocator, cxlTkn);
        if (!readResult.Success) return StoreFailure(readResult.Errors);

        FileStorageResult storeResult;
        await using (readResult.FileStream)
        {
            storeResult = await strategy.StoreAsync(
                readResult.FileStream,
                FileResourceType.Document,
                GetStorageFileName(document),
                FileDirectoryCategory.Received,
                cxlTkn);
        }

        if (!storeResult.Success) return StoreFailure(storeResult.Errors);

        var stagingLocator = document.StagingLocator;
        document.StorageLocator = storeResult.Locator;
        document.StorageMethod = strategy.Method;
        document.StagingLocator = null;
        await documentService.UpdateAsync(document, cxlTkn);

        await TryDeleteStagingAsync(stagingLocator);

        return new FileStorageResult
        {
            Success = true,
            Locator = document.StorageLocator,
        };
    }

    public async Task<FileReadResult> OpenReadAsync(
        Guid documentId,
        CancellationToken cxlTkn = default)
    {
        var document = await GetDocumentAsync(documentId, cxlTkn);
        if (document == null) return ReadFailure("No document matched the provided ID.");

        if (document.StorageMethod == FileStorageMethod.Unspecified)
        {
            return string.IsNullOrWhiteSpace(document.StagingLocator)
                ? ReadFailure("Document has no staged file.")
                : await stagingStore.OpenReadAsync(document.StagingLocator, cxlTkn);
        }

        if (string.IsNullOrWhiteSpace(document.StorageLocator))
            return ReadFailure("Document has no durable storage locator.");

        var strategy = GetStrategy(document.StorageMethod);
        if (strategy == null)
            return ReadFailure($"Unsupported document storage method '{document.StorageMethod}'.");

        return await strategy.OpenReadAsync(document.StorageLocator, cxlTkn);
    }

    public async Task<FileStorageResult> MoveAsync(
        Guid documentId,
        FileDirectoryCategory newDirectory,
        CancellationToken cxlTkn = default)
    {
        var document = await GetDocumentAsync(documentId, cxlTkn);
        if (document == null) return StoreFailure("No document matched the provided ID.");

        if (document.StorageMethod == FileStorageMethod.Unspecified)
            return StoreFailure("Cannot move a document that has not been persisted to durable storage.");

        if (string.IsNullOrWhiteSpace(document.StorageLocator))
            return StoreFailure("Document has no durable storage locator.");

        var strategy = GetStrategy(document.StorageMethod);
        if (strategy == null)
            return StoreFailure($"Unsupported document storage method '{document.StorageMethod}'.");

        var result = await strategy.MoveAsync(document.StorageLocator, newDirectory, cxlTkn);
        if (!result.Success) return StoreFailure(result.Errors);

        document.StorageLocator = result.Locator;
        await documentService.UpdateAsync(document, cxlTkn);

        return new FileStorageResult
        {
            Success = true,
            Locator = document.StorageLocator,
        };
    }

    public async Task<FileDeletionResult> DeleteAsync(
        Guid documentId,
        CancellationToken cxlTkn = default)
    {
        var document = await GetDocumentAsync(documentId, cxlTkn);
        if (document == null) return DeleteFailure("No document matched the provided ID.");

        if (document.StorageMethod == FileStorageMethod.Unspecified)
            return DeleteFailure("Cannot delete a document that has not been persisted to durable storage.");

        if (string.IsNullOrWhiteSpace(document.StorageLocator))
            return DeleteFailure("Document has no durable storage locator.");

        var strategy = GetStrategy(document.StorageMethod);
        if (strategy == null)
            return DeleteFailure($"Unsupported document storage method '{document.StorageMethod}'.");

        var result = await strategy.DeleteAsync(document.StorageLocator, cxlTkn);
        if (!result.Success) return result;

        document.StorageLocator = null;
        document.StorageMethod = FileStorageMethod.Unspecified;
        await documentService.UpdateAsync(document, cxlTkn);
        return result;
    }

    private Task<Document?> GetDocumentAsync(Guid documentId, CancellationToken cxlTkn) =>
        documentService.GetByIdAsync(documentId, cancellationToken: cxlTkn);

    private IDurableStorageStrategy? GetStrategy(FileStorageMethod storageMethod) => storageMethod switch
    {
        FileStorageMethod.LocalHardDrive => localHardDriveStrategy,
        FileStorageMethod.Supabase => supabaseStrategy,
        _ => null,
    };

    private static string GetStorageFileName(Document document) =>
        $"{document.Id}{FileHelper.GetCanonicalExtension(document.FileType)}";

    private async Task TryDeleteStagingAsync(string stagingLocator)
    {
        try
        {
            await stagingStore.DeleteAsync(stagingLocator, CancellationToken.None);
        }
        catch (Exception)
        {
            // Do not abort on cleanup failure
            // Durable storage state is already committed
            // Scheduled cleanup remains the fallback
        }
    }

    private static FileStorageResult StoreFailure(string[] errors) => new() { Success = false, Errors = errors };
    private static FileStorageResult StoreFailure(string error) => new() { Success = false, Errors = [error] };
    private static FileReadResult ReadFailure(string error) => new() { Success = false, Errors = [error] };
    private static FileDeletionResult DeleteFailure(string error) => new() { Success = false, Errors = [error] };
}
