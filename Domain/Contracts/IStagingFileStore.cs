using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IStagingFileStore
{
    Task<bool> ExistsAsync(string locator, CancellationToken cancellationToken = default);

    Task<FileLocatorResult> StageAsync(
        Stream content,
        string canonicalExtension,
        CancellationToken cancellationToken = default);

    Task<FileReadResult> OpenReadAsync(string locator, CancellationToken cancellationToken = default);

    Task<FileDeletionResult> DeleteAsync(string locator, CancellationToken cancellationToken = default);
}
