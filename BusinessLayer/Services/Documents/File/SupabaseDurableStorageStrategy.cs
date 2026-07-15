using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.Extensions.Options;
using Supabase.Storage;

namespace Business.Services.Documents.File;

public sealed class SupabaseDurableStorageStrategy(
    Supabase.Client supabase,
    IOptions<SupabaseOptions> supabaseOpts,
    IOptions<FileStorageOptions> storageOpts,
    ILocalFileBuffer localFileBuffer) : IDurableStorageStrategy
{
    private readonly Supabase.Client _supabase = supabase;
    private readonly SupabaseOptions _supabaseOpts = supabaseOpts.Value;
    private readonly FileStorageOptions _storageOpts = storageOpts.Value;
    private readonly ILocalFileBuffer _localFileBuffer = localFileBuffer;

    public DocumentStorageMethod Method => DocumentStorageMethod.Supabase;

    public async Task<bool> ExistsAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(locator)) return false;

        try
        {
            var (directory, fileName) = SplitLocator(locator);
            var files = await _supabase.Storage.From(_supabaseOpts.DocumentBucket).List(
                path: directory,
                options: new SearchOptions { Search = fileName });
            return files?.Any(file => string.Equals(file.Name, fileName, StringComparison.Ordinal)) == true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return false;
        }
    }

    public async Task<FileLocatorResult> StoreAsync(
        Stream content,
        string storageName,
        DocumentFileDirectory directory,
        CancellationToken cxlTkn = default)
    {
        try
        {
            await using var lease = await _localFileBuffer.CopyFromAsync(
                content,
                Path.GetExtension(storageName),
                cxlTkn);

            var locator = Combine(GetStorageDirectory(directory), storageName);
            var fullPath = await _supabase.Storage.From(_supabaseOpts.DocumentBucket).Upload(
                lease.FilePath,
                locator,
                new Supabase.Storage.FileOptions { Upsert = true });

            if (string.IsNullOrWhiteSpace(fullPath))
                return Failure($"Failed to store file in Supabase bucket '{_supabaseOpts.DocumentBucket}'.");

            return Success(GetLocatorInBucket(fullPath));
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
            var actualPath = await _supabase.Storage.From(_supabaseOpts.DocumentBucket)
                .Download(locator, lease.FilePath, onProgress: null);
            if (string.IsNullOrWhiteSpace(actualPath))
            {
                await lease.DisposeAsync();
                return ReadFailure($"Failed to read file from Supabase bucket '{_supabaseOpts.DocumentBucket}'.");
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

    public async Task<FileLocatorResult> MoveAsync(
        string locator,
        DocumentFileDirectory directory,
        CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        try
        {
            var (_, fileName) = SplitLocator(locator);
            var destination = Combine(GetStorageDirectory(directory), fileName);

            if (string.Equals(locator, destination, StringComparison.Ordinal))
                return Success(destination);

            var moved = await _supabase.Storage.From(_supabaseOpts.DocumentBucket).Move(locator, destination);
            return moved
                ? Success(destination)
                : Failure($"Failed to move file in Supabase bucket '{_supabaseOpts.DocumentBucket}'.");
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
            var result = await _supabase.Storage.From(_supabaseOpts.DocumentBucket).Remove(locator);
            return result == null
                ? DeleteFailure($"Failed to delete file from Supabase bucket '{_supabaseOpts.DocumentBucket}'.")
                : new FileDeletionResult { Success = true };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return DeleteFailure(ex.Message);
        }
    }

    private string GetStorageDirectory(DocumentFileDirectory directory) => directory switch
    {
        DocumentFileDirectory.Received => _storageOpts.FileDirectoryReceived,
        DocumentFileDirectory.Processing => _storageOpts.FileDirectoryProcessing,
        DocumentFileDirectory.Indexed => _storageOpts.FileDirectoryIndexed,
        DocumentFileDirectory.Failed => _storageOpts.FileDirectoryFailed,
        _ => throw new ArgumentOutOfRangeException(nameof(directory)),
    };

    private static (string Directory, string FileName) SplitLocator(string locator)
    {
        var separatorIndex = locator.LastIndexOf('/');
        return separatorIndex < 0
            ? (string.Empty, locator)
            : (locator[..separatorIndex], locator[(separatorIndex + 1)..]);
    }

    private static string Combine(string directory, string fileName) =>
        string.IsNullOrWhiteSpace(directory) ? fileName : $"{directory.TrimEnd('/')}/{fileName}";

    private static string GetLocatorInBucket(string fullPath)
    {
        var separatorIndex = fullPath.IndexOf('/');
        return separatorIndex < 0 ? fullPath : fullPath[(separatorIndex + 1)..];
    }

    private static FileLocatorResult Success(string locator) => new() { Success = true, Locator = locator };
    private static FileLocatorResult Failure(string error) => new() { Success = false, Errors = [error] };
    private static FileReadResult ReadFailure(string error) => new() { Success = false, Errors = [error] };
    private static FileDeletionResult DeleteFailure(string error) => new() { Success = false, Errors = [error] };
}
