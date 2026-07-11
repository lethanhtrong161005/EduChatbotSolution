using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Entities;
using Microsoft.Extensions.Options;

namespace Business.Services.Documents.File;

public sealed class LocalHardDriveDurableStorageStrategy(
    IOptions<FileStorageOptions> storageOpts) : IDurableStorageStrategy
{
    private readonly FileStorageOptions _storageOpts = storageOpts.Value;

    public DocumentStorageMethod Method => DocumentStorageMethod.LocalHardDrive;

    public Task<bool> ExistsAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();
        return Task.FromResult(IsOwnedLocator(locator) && System.IO.File.Exists(locator));
    }

    public async Task<FileLocatorResult> StoreAsync(
        Stream content,
        string storageName,
        DocumentFileDirectory directory,
        CancellationToken cxlTkn = default)
    {
        try
        {
            var destinationDirectory = GetPhysicalDirectory(directory);
            Directory.CreateDirectory(destinationDirectory);
            var locator = Path.Combine(destinationDirectory, storageName);

            await using var destination = new FileStream(
                locator,
                FileMode.Create,
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
            return Task.FromResult(ReadFailure($"Document file not found at '{locator}'."));

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

    public Task<FileLocatorResult> MoveAsync(
        string locator,
        DocumentFileDirectory directory,
        CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        if (!IsOwnedLocator(locator) || !System.IO.File.Exists(locator))
            return Task.FromResult(Failure($"Document file not found at '{locator}'."));

        try
        {
            var destinationDirectory = GetPhysicalDirectory(directory);
            Directory.CreateDirectory(destinationDirectory);
            var destination = Path.Combine(destinationDirectory, Path.GetFileName(locator));

            if (!string.Equals(locator, destination, StringComparison.OrdinalIgnoreCase))
                System.IO.File.Move(locator, destination, overwrite: true);

            return Task.FromResult(Success(destination));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Failure(ex.Message));
        }
    }

    public Task<FileDeletionResult> DeleteAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        if (!IsOwnedLocator(locator))
            return Task.FromResult(DeleteFailure("Durable locator is outside the configured storage root."));

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

    private static readonly string AppData = Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData,
        Environment.SpecialFolderOption.DoNotVerify);

    private string GetStorageRoot() => Path.GetFullPath(Path.Combine(AppData, _storageOpts.AppDirectory));

    private bool IsOwnedLocator(string locator)
    {
        if (string.IsNullOrWhiteSpace(locator)) return false;
        try
        {
            var rootPrefix = GetStorageRoot().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                             + Path.DirectorySeparatorChar;

            return Path.GetFullPath(locator).StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private string GetPhysicalDirectory(DocumentFileDirectory directory) => directory switch
    {
        DocumentFileDirectory.Received => Path.Combine(GetStorageRoot(), _storageOpts.FileDirectoryReceived),
        DocumentFileDirectory.Processing => Path.Combine(GetStorageRoot(), _storageOpts.FileDirectoryProcessing),
        DocumentFileDirectory.Indexed => Path.Combine(GetStorageRoot(), _storageOpts.FileDirectoryIndexed),
        DocumentFileDirectory.Failed => Path.Combine(GetStorageRoot(), _storageOpts.FileDirectoryFailed),
        _ => throw new ArgumentOutOfRangeException(nameof(directory)),
    };

    private static FileLocatorResult Success(string locator) => new() { Success = true, Locator = locator };
    private static FileLocatorResult Failure(string error) => new() { Success = false, Errors = [error] };
    private static FileReadResult ReadFailure(string error) => new() { Success = false, Errors = [error] };
    private static FileDeletionResult DeleteFailure(string error) => new() { Success = false, Errors = [error] };
}
