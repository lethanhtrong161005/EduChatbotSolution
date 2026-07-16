using Business.Services.Documents.File;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Utils;
using Microsoft.Extensions.Options;
using Supabase.Storage;

namespace Business.Services.Storage;

public sealed class SupabaseDurableStorageStrategy(
    Supabase.Client supabase,
    IOptions<SupabaseOptions> supabaseOpts,
    IOptions<GeneralDriveStorageOptions> generalStorageOpts,
    IOptions<DocumentFileStorageOptions> docStorageOpts,
    ILocalFileBuffer localFileBuffer)
    : IDurableStorageStrategy
{
    private readonly Supabase.Client _supabase = supabase;
    private readonly SupabaseOptions _supabaseOpts = supabaseOpts.Value;
    private readonly GeneralDriveStorageOptions _generalStorageOpts = generalStorageOpts.Value;
    private readonly DocumentFileStorageOptions _docStorageOpts = docStorageOpts.Value;
    private readonly ILocalFileBuffer _localFileBuffer = localFileBuffer;

    public FileStorageMethod Method => FileStorageMethod.Supabase;

    public async Task<bool> ExistsAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(locator)) return false;

        try
        {
            var (bucket, directory, fileName) = SplitLocator(locator);
            var files = await _supabase.Storage.From(bucket).List(
                path: directory,
                options: new SearchOptions { Search = fileName });
            return files?.Any(file => string.Equals(file.Name, fileName, StringComparison.Ordinal)) == true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    public async Task<FileStorageResult> StoreAsync(
        Stream content,
        FileResourceType resourceType,
        string storageName,
        FileDirectoryCategory directoryCategory,
        CancellationToken cxlTkn = default)
    {
        try
        {
            await using var lease = await _localFileBuffer.CopyFromAsync(
                content,
                Path.GetExtension(storageName),
                cxlTkn);

            var bucket = GetBucketName(resourceType);
            var objectKey = PathCombine(GetStorageDirectory(directoryCategory), storageName);
            var fullPath = await _supabase.Storage.From(bucket).Upload(
                lease.FilePath,
                objectKey,
                new Supabase.Storage.FileOptions { Upsert = true });

            if (string.IsNullOrWhiteSpace(fullPath))
                return Failure($"Failed to store file in Supabase bucket '{bucket}'.");

            return Success(fullPath);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Failure(ex.Message);
        }
    }

    public async Task<FileReadResult> OpenReadAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        var lease = await _localFileBuffer.AllocateAsync(Path.GetExtension(locator), cxlTkn);

        try
        {
            var (bucket, directory, filename) = SplitLocator(locator);
            var actualPath = await _supabase.Storage.From(bucket)
                .Download(PathCombine(directory, filename), lease.FilePath, onProgress: null);
            if (string.IsNullOrWhiteSpace(actualPath))
            {
                await lease.DisposeAsync();
                return ReadFailure($"Failed to read file from Supabase bucket '{bucket}'.");
            }

            var stream = lease.OpenReadAndTransferOwnership();
            await lease.DisposeAsync();
            return new FileReadResult { Success = true, FileStream = stream };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await lease.DisposeAsync();
            return ReadFailure(ex.Message);
        }
    }

    public async Task<FileStorageResult> MoveAsync(
        string locator,
        FileDirectoryCategory directoryCategory,
        CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        try
        {
            var (bucket, oldDirectory, fileName) = SplitLocator(locator);
            var newDirectory = GetStorageDirectory(directoryCategory);
            var newObjectKey = PathCombine(newDirectory, fileName);
            var newLocator = PathCombine(bucket, newObjectKey);

            if (string.Equals(locator, newLocator, StringComparison.Ordinal))
                return Success(newLocator);

            var oldObjectKey = PathCombine(oldDirectory, fileName);
            var moved = await _supabase.Storage.From(bucket).Move(oldObjectKey, newObjectKey);
            return moved
                ? Success(newLocator)
                : Failure($"Failed to move file in Supabase bucket '{bucket}'.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Failure(ex.Message);
        }
    }

    public async Task<FileDeletionResult> DeleteAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        try
        {
            var (bucket, directory, filename) = SplitLocator(locator);
            var result = await _supabase.Storage.From(bucket).Remove(PathCombine(directory, filename));
            return result == null
                ? DeleteFailure($"Failed to delete file from Supabase bucket '{bucket}'.")
                : new FileDeletionResult { Success = true };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return DeleteFailure(ex.Message);
        }
    }

    private string GetBucketName(FileResourceType resourceType) => resourceType switch
    {
        FileResourceType.Document => _supabaseOpts.DocumentBucket,
        FileResourceType.UserImportBatch => _supabaseOpts.UserImportBucket,
        _ => throw new ArgumentOutOfRangeException(nameof(resourceType)),
    };

    private string GetStorageDirectory(FileDirectoryCategory directory) => directory switch
    {
        FileDirectoryCategory.Received => _generalStorageOpts.FileDirectoryReceived,
        FileDirectoryCategory.Processing => _generalStorageOpts.FileDirectoryProcessing,
        FileDirectoryCategory.Indexed => _generalStorageOpts.FileDirectoryIndexed,
        FileDirectoryCategory.Failed => _generalStorageOpts.FileDirectoryFailed,
        _ => throw new ArgumentOutOfRangeException(nameof(directory)),
    };

    private static (string Bucket, string Directory, string FileName) SplitLocator(string locator)
    {
        var firstSeparatorIndex = locator.IndexOf('/');
        if (firstSeparatorIndex < 0) throw new ArgumentException("Locator contains no bucket information.");
        var bucket = locator[..firstSeparatorIndex];
        var objectKey = locator[(firstSeparatorIndex + 1)..];

        var lastSeparatorIndex = objectKey.LastIndexOf('/');
        return lastSeparatorIndex < 0
            ? (bucket, string.Empty, objectKey)
            : (bucket, objectKey[..lastSeparatorIndex], objectKey[(lastSeparatorIndex + 1)..]);
    }

    private static string PathCombine(string segment1, string segment2) =>
        string.IsNullOrWhiteSpace(segment1) ? segment2 : $"{segment1.TrimEnd('/')}/{segment2}";

    private static FileStorageResult Success(string locator) => new() { Success = true, Locator = locator };
    private static FileStorageResult Failure(string error) => new() { Success = false, Errors = [error] };
    private static FileReadResult ReadFailure(string error) => new() { Success = false, Errors = [error] };
    private static FileDeletionResult DeleteFailure(string error) => new() { Success = false, Errors = [error] };
}
