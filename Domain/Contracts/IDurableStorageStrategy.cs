using Domain.Contracts.DTOs;
using Domain.Utils;

namespace Domain.Contracts;

public interface IDurableStorageStrategy
{
    FileStorageMethod Method { get; }

    Task<bool> ExistsAsync(string locator, CancellationToken cancellationToken = default);

    Task<FileStorageResult> StoreAsync(
        Stream content,
        FileResourceType resourceType,
        string storageName,
        FileDirectoryCategory directoryCategory,
        CancellationToken cancellationToken = default);

    Task<FileReadResult> OpenReadAsync(string locator, CancellationToken cancellationToken = default);

    Task<FileStorageResult> MoveAsync(
        string locator,
        FileDirectoryCategory directoryCategory,
        CancellationToken cancellationToken = default);

    Task<FileDeletionResult> DeleteAsync(string locator, CancellationToken cancellationToken = default);
}
