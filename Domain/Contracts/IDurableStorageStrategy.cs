using Domain.Contracts.DTOs;
using Domain.Entities;

namespace Domain.Contracts;

public interface IDurableStorageStrategy
{
    DocumentStorageMethod Method { get; }

    Task<bool> ExistsAsync(string locator, CancellationToken cancellationToken = default);

    Task<FileLocatorResult> StoreAsync(
        Stream content,
        string storageName,
        DocumentFileDirectory directory,
        CancellationToken cancellationToken = default);

    Task<FileReadResult> OpenReadAsync(string locator, CancellationToken cancellationToken = default);

    Task<FileLocatorResult> MoveAsync(
        string locator,
        DocumentFileDirectory directory,
        CancellationToken cancellationToken = default);

    Task<FileDeletionResult> DeleteAsync(string locator, CancellationToken cancellationToken = default);
}
