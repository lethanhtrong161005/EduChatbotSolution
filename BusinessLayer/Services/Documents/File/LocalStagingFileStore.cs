using Domain.Contracts;
using Domain.Contracts.DTOs;
using Microsoft.Extensions.Options;

namespace Business.Services.Documents.File;

public sealed class LocalStagingFileStore(
    IOptions<FileStorageOptions> storageOpts)
    : IStagingFileStore
{
    private readonly FileStorageOptions _storageOpts = storageOpts.Value;

    public Task<bool> ExistsAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();
        return Task.FromResult(IsOwnedLocator(locator) && System.IO.File.Exists(locator));
    }

    public async Task<FileLocatorResult> StageAsync(
        Stream content,
        string requestedExtension,
        CancellationToken cxlTkn = default)
    {
        try
        {
            var root = GetStagingRoot();
            Directory.CreateDirectory(root);
            var locator = Path.Combine(root, $"{Guid.NewGuid()}{NormalizeExtension(requestedExtension)}");

            await using var destination = new FileStream(
                locator,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await content.CopyToAsync(destination, cxlTkn);
            return Success(locator);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Failure(ex.Message);
        }
    }

    public Task<FileReadResult> OpenReadAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        if (!IsOwnedLocator(locator) || !System.IO.File.Exists(locator))
            return Task.FromResult(ReadFailure($"Staged file could not be found at '{locator}'."));

        try
        {
            return Task.FromResult(new FileReadResult
            {
                Success = true,
                FileStream = new FileStream(locator, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true),
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(ReadFailure(ex.Message));
        }
    }

    public Task<FileDeletionResult> DeleteAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        if (!IsOwnedLocator(locator))
            return Task.FromResult(DeleteFailure("Staging locator is outside the configured staging root."));

        try
        {
            System.IO.File.Delete(locator);
            return Task.FromResult(new FileDeletionResult { Success = true });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(DeleteFailure(ex.Message));
        }
    }

    private string GetStagingRoot() => Path.GetFullPath(Path.Combine(
        Path.GetTempPath(),
        _storageOpts.AppDirectory,
        _storageOpts.FileDirectoryStaging));

    private bool IsOwnedLocator(string locator)
    {
        if (string.IsNullOrWhiteSpace(locator)) return false;
        try
        {
            var rootPrefix = GetStagingRoot().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;

            return Path.GetFullPath(locator).StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static string NormalizeExtension(string extension) =>
        string.IsNullOrWhiteSpace(extension) ? string.Empty : "." + extension.Trim().TrimStart('.');

    private static FileLocatorResult Success(string locator) => new() { Success = true, Locator = locator };
    private static FileLocatorResult Failure(string error) => new() { Success = false, Errors = [error] };
    private static FileReadResult ReadFailure(string error) => new() { Success = false, Errors = [error] };
    private static FileDeletionResult DeleteFailure(string error) => new() { Success = false, Errors = [error] };
}
