using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IStagingFileStore
{
    Task<bool> ExistsAsync(string locator, CancellationToken cancellationToken = default);

    Task<FileStorageResult> StageAsync(
        Stream content,
        string requestedExtension,
        CancellationToken cancellationToken = default);

    Task<FileReadResult> OpenReadAsync(string locator, CancellationToken cancellationToken = default);

    Task<FileDeletionResult> DeleteAsync(string locator, CancellationToken cancellationToken = default);
}
