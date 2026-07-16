using Business.Services.Account;
using Business.Services.Documents.File;
using Domain.Contracts;
using Domain.Contracts.DTOs;
using Domain.Utils;
using Microsoft.Extensions.Options;

namespace Business.Services.Storage;

public sealed class LocalHardDriveDurableStorageStrategy(
    IOptions<GeneralDriveStorageOptions> generalStorageOpts,
    IOptions<DocumentFileStorageOptions> documentStorageOpts,
    IOptions<UserImportFileStorageOptions> userImportStorageOpts)
    : IDurableStorageStrategy
{
    private readonly GeneralDriveStorageOptions _generalStorageOpts = generalStorageOpts.Value;
    private readonly DocumentFileStorageOptions _documentStorageOpts = documentStorageOpts.Value;
    private readonly UserImportFileStorageOptions _userImportStorageOpts = userImportStorageOpts.Value;

    public FileStorageMethod Method => FileStorageMethod.LocalHardDrive;

    public Task<bool> ExistsAsync(string locator, CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();
        return Task.FromResult(IsOwnedLocator(locator) && System.IO.File.Exists(locator));
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
            var destinationDirectory = Path.Combine(GetStorageRoot(), GetResourceDirectory(resourceType), GetPhysicalSubDirectory(directoryCategory));
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

    public Task<FileStorageResult> MoveAsync(
        string locator,
        FileDirectoryCategory directoryCategory,
        CancellationToken cxlTkn = default)
    {
        cxlTkn.ThrowIfCancellationRequested();

        if (!IsOwnedLocator(locator) || !System.IO.File.Exists(locator))
            return Task.FromResult(Failure($"Document file not found at '{locator}'."));

        try
        {
            var (root, resource, _) = SplitLocator(locator);
            var destinationDirectory = Path.Combine(root, resource, GetPhysicalSubDirectory(directoryCategory));
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

    private string GetStorageRoot() => Path.GetFullPath(Path.Combine(AppData, _generalStorageOpts.AppDirectory));

    private bool IsOwnedLocator(string locator)
    {
        if (string.IsNullOrWhiteSpace(locator)) return false;
        try
        {
            var rootPrefix = GetStorageRoot().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            return Path.GetFullPath(locator).StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private string GetResourceDirectory(FileResourceType resourceType) => resourceType switch
    {
        FileResourceType.Document => _documentStorageOpts.ResourceDirectory,
        FileResourceType.UserImportBatch => _userImportStorageOpts.ResourceDirectory,
        _ => throw new ArgumentOutOfRangeException(nameof(resourceType)),
    };

    private string GetPhysicalSubDirectory(FileDirectoryCategory directory) => directory switch
    {
        FileDirectoryCategory.Received => _generalStorageOpts.FileDirectoryReceived,
        FileDirectoryCategory.Processing => _generalStorageOpts.FileDirectoryProcessing,
        FileDirectoryCategory.Indexed => _generalStorageOpts.FileDirectoryIndexed,
        FileDirectoryCategory.Failed => _generalStorageOpts.FileDirectoryFailed,
        _ => throw new ArgumentOutOfRangeException(nameof(directory)),
    };

    private (string Root, string Resource, string Path) SplitLocator(string locator)
    {
        if (!IsOwnedLocator(locator)) throw new ArgumentException("Invalid durable locator: Outside the configured storage root.");

        var root = GetStorageRoot();
        var rest = locator[(root.Length + 1)..];
        var resourceSeparatorIndex = rest.IndexOf(Path.DirectorySeparatorChar);
        if (resourceSeparatorIndex < 0) throw new ArgumentException("Invalid durable locator: Missing resource type directory.");

        return (root, rest[..resourceSeparatorIndex], rest[(resourceSeparatorIndex + 1)..]);
    }

    private static FileStorageResult Success(string locator) => new() { Success = true, Locator = locator };
    private static FileStorageResult Failure(string error) => new() { Success = false, Errors = [error] };
    private static FileReadResult ReadFailure(string error) => new() { Success = false, Errors = [error] };
    private static FileDeletionResult DeleteFailure(string error) => new() { Success = false, Errors = [error] };
}
