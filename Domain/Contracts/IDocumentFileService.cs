using Domain.Contracts.DTOs;

namespace Domain.Contracts;

public interface IDocumentFileService
{
    Task<bool> ExistsAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<FileLocatorResult> PersistAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<FileReadResult> OpenReadAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<FileLocatorResult> MoveAsync(Guid documentId, DocumentFileDirectory newDirectory, CancellationToken cancellationToken = default);

    Task<FileDeletionResult> DeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}

public enum DocumentFileDirectory
{
    Received,
    Processing,
    Indexed,
    Failed,
}
